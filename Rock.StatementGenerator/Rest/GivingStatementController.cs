// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Web.Http;
using System.Linq;
using Rock.Web.Cache;
using Rock.Rest.Filters;
using Rock.Rest;
using Rock.Lava;
using Rock.Data;
using Rock.Security;
using System.Net.Http;
using System.Net.Http.Headers;
using Rock.Model;
using System.Data.Entity;

namespace Rock.StatementGenerator.Rest
{
    /// <summary>
    /// This controller is to provide a giving statement for a given person and year
    /// </summary>
    public class GivingStatementController : ApiControllerBase
    {
        /// <summary>
        /// Render and return a giving statement for the specified person.
        /// </summary>
        /// <param name="personId">The person that made the contributions. That person's entire
        /// giving group is included, which is typically the family.</param>
        /// <param name="year">The contribution calendar year. ie 2019.  If not specified, the
        /// current year is assumed.</param>
        /// <param name="templateDefinedValueId">The defined value ID that represents the statement
        /// lava. This defined value should be a part of the Statement Generator Lava Template defined
        /// type. If no ID is specified, then the default defined value for the Statement Generator Lava
        /// Template defined type is assumed.</param>
        /// <param name="additionalMergeObjects">Use this parameter to pass in additional merge fields
        /// for the lava template that are not included in the default Statement Generator Lava
        /// Template lava</param>
        /// <returns>The rendered giving statement</returns>
        [System.Web.Http.Route( "api/GivingStatement/{personId}" )]
        [HttpGet]
        [Authenticate, Secured]
        public HttpResponseMessage Render( int personId, [FromUri] int? year = null, [FromUri] int? templateDefinedValueId = null, [FromUri] string additionalMergeObjects = null )
        {
            // Assume the current year if no year is specified
            var currentYear = RockDateTime.Now.Year;
            year = year ?? currentYear;
            var isCurrentYear = year == currentYear;

            // Load the statement lava defined type
            var definedTypeCache = DefinedTypeCache.Get( SystemGuid.DefinedType.STATEMENT_GENERATOR_LAVA_TEMPLATE );
            if ( definedTypeCache == null )
            {
                throw new Exception( "The defined type 'Statement Generator Lava Template' could not be found." );
            }

            // Get the specified defined value or the default if none is specified
            var templateValues = definedTypeCache.DefinedValues;
            var templateValue = templateDefinedValueId.HasValue ?
                templateValues.FirstOrDefault( dv => dv.Id == templateDefinedValueId.Value ) :
                templateValues.OrderBy( dv => dv.Order ).FirstOrDefault();

            if ( templateValue == null )
            {
                throw new Exception( string.Format(
                    "The defined value '{0}' within 'Statement Generator Lava Template' could not be found.",
                    templateDefinedValueId.HasValue ?
                        templateDefinedValueId.Value.ToString() :
                        "default" ) );
            }

            // The lava template is an attribute of the defined value
            var template = templateValue.GetAttributeValue( "LavaTemplate" );
            if ( string.IsNullOrEmpty( template ) )
            {
                throw new Exception( "The template cannot be null or empty" );
            }

            // Get common merge fields and add them to the dictionary
            var currentPerson = GetPerson();
            var mergeFields = LavaHelper.GetCommonMergeFields( null, currentPerson, new CommonMergeFieldsOptions
            {
                GetPageContext = false,
                GetPageParameters = false,
                GetCurrentPerson = true,
                GetCampuses = true,
                GetLegacyGlobalMergeFields = false
            } );
            mergeFields.Add( "LavaTemplate", templateValue );
            mergeFields.Add( "StatementStartDate", string.Format( "1/1/{0}", year ) );
            mergeFields.Add( "StatementEndDate", isCurrentYear ?
                RockDateTime.Now.ToShortDateString() :
                string.Format( "12/31/{0}", year ) );

            // Declare the necessary services
            var rockContext = new RockContext();
            var financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );
            var personService = new PersonService( rockContext );
            var personAliasService = new PersonAliasService( rockContext );
            var groupMemberService = new GroupMemberService( rockContext );

            // Get the person that the giving statement is for
            var person = personService.Get( personId );
            if ( person == null )
            {
                throw new Exception( string.Format( "The person with ID {0} could not be found", personId ) );
            }
            
            // Include all the people with the same giving ID
            var personAliasIds = personAliasService.Queryable()
                .AsNoTracking()
                .Where( a => a.Person.GivingId == person.GivingId )
                .Select( a => a.Id )
                .ToList();

            // Get the transactions for the giving group for the specified year
            var transactionDetailsQry = financialTransactionDetailService.Queryable()
                .AsNoTracking()
                .Where( t =>
                    t.Transaction.AuthorizedPersonAliasId.HasValue &&
                    personAliasIds.Contains( t.Transaction.AuthorizedPersonAliasId.Value ) &&
                    t.Transaction.TransactionDateTime.HasValue &&
                    t.Transaction.TransactionDateTime.Value.Year == year &&
                    t.Account.IsTaxDeductible )
                .OrderByDescending( t => t.Transaction.TransactionDateTime );

            mergeFields.Add( "TransactionDetails", transactionDetailsQry.ToList() );
            mergeFields.Add( "TotalContributionAmount",  );

            // Get the names of the people in the giving group
            var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
            if (familyGroupType == null)
            {
                throw new Exception( "The family group type could not be found" );
            }
            
            var groupMemberQry = groupMemberService.Queryable()
                .AsNoTracking()
                .Where( m => m.Group.GroupTypeId == familyGroupType.Id );

            // Get giving group members in order by family role (adult -> child) and then gender (male -> female)
            var givingGroup = personService.Queryable()
                .AsNoTracking()
                .Where( p => p.GivingId == person.GivingId )
                .GroupJoin( groupMemberQry,
                    p => p.Id,
                    m => m.PersonId,
                    ( p, m ) => new { p, m } )
                .SelectMany( x => x.m.DefaultIfEmpty(), ( y, z ) => new { Person = y.p, GroupMember = z } )
                .Select( p => new {
                    FirstName = p.Person.NickName,
                    LastName = p.Person.LastName,
                    FamilyRoleOrder = p.GroupMember.GroupRole.Order,
                    Gender = p.Person.Gender,
                    PersonId = p.Person.Id } )
                .DistinctBy( p => p.PersonId )
                .OrderBy( p => p.FamilyRoleOrder )
                .ThenBy( p => p.Gender )
                .ToList();

            // Calculate the salutation based on the people in the giving group and their standing
            var salutation = string.Empty;

            if ( givingGroup.GroupBy( g => g.LastName ).Count() == 1 )
            {
                salutation = string.Join( ", ", givingGroup.Select( g => g.FirstName ) ) + " " + givingGroup.FirstOrDefault().LastName;                
            }
            else
            {
                salutation = string.Join( ", ", givingGroup.Select( g => g.FirstName + " " + g.LastName ) );                
            }

            if ( salutation.Contains( "," ) )
            {
                salutation = salutation.ReplaceLastOccurrence( ",", " &" );
            }

            mergeFields.Add( "Salutation", salutation );

            // Add the mailing address as a merge field
            var mailingAddress = person.GetMailingLocation();
            if ( mailingAddress != null )
            {
                mergeFields.Add( "StreetAddress1", mailingAddress.Street1 );
                mergeFields.Add( "StreetAddress2", mailingAddress.Street2 );
                mergeFields.Add( "City", mailingAddress.City );
                mergeFields.Add( "State", mailingAddress.State );
                mergeFields.Add( "PostalCode", mailingAddress.PostalCode );
                mergeFields.Add( "Country", mailingAddress.Country );
            }
            else
            {
                mergeFields.Add( "StreetAddress1", string.Empty );
                mergeFields.Add( "StreetAddress2", string.Empty );
                mergeFields.Add( "City", string.Empty );
                mergeFields.Add( "State", string.Empty );
                mergeFields.Add( "PostalCode", string.Empty );
                mergeFields.Add( "Country", string.Empty );
            }

            // Get the financial accounts
            var accountSummaries = transactionDetailsQry
                .GroupBy( t => new { t.Account.Name, t.Account.PublicName, t.Account.Description } )
                .Select( s => new AccountSummary
                {
                    AccountName = s.Key.Name,
                    PublicName = s.Key.PublicName,
                    Description = s.Key.Description,
                    Total = s.Sum( a => a.Amount ),
                    Order = s.Max( a => a.Account.Order )
                } )
                .OrderBy( s => s.Order )
                .ToList();

            mergeFields.Add( "AccountSummary", accountSummaries );

            // Apply the specified merge fields if they were supplied
            if ( additionalMergeObjects != null )
            {
                var additionalMergeObjectList = additionalMergeObjects.Split( ',' )
                    .Select( a => a.Split( '|' ) )
                    .Where( a => a.Length == 3 )
                    .Select( a => new
                    {
                        EntityTypeId = a[0].AsInteger(),
                        MergeKey = a[1],
                        EntityId = a[2].AsInteger()
                    } ).ToList();

                foreach ( var additionalMergeObject in additionalMergeObjectList )
                {
                    var entityTypeType = EntityTypeCache.Get( additionalMergeObject.EntityTypeId )?.GetEntityType();
                    if ( entityTypeType == null )
                    {
                        continue;
                    }

                    var dbContext = Reflection.GetDbContextForEntityType( entityTypeType );
                    var serviceInstance = Reflection.GetServiceForEntityType( entityTypeType, dbContext );
                    if ( serviceInstance == null )
                    {
                        continue;
                    }

                    var getMethod = serviceInstance.GetType().GetMethod( "Get", new Type[] { typeof( int ) } );
                    var mergeObjectEntity = getMethod.Invoke( serviceInstance, new object[] { additionalMergeObject.EntityId } ) as IEntity;
                    if ( mergeObjectEntity == null )
                    {
                        continue;
                    }

                    var canView = true;
                    var securedEntity = mergeObjectEntity as ISecured;
                    if ( securedEntity != null )
                    {
                        canView = securedEntity.IsAuthorized( Authorization.VIEW, currentPerson );
                    }

                    if ( canView )
                    {
                        mergeFields.Add( additionalMergeObject.MergeKey, mergeObjectEntity );
                    }
                }
            }

            // Render the statement and send back to the user
            var body = template.ResolveMergeFields( mergeFields, currentPerson );
            var response = new HttpResponseMessage();
            response.Content = new StringContent( body );
            response.Content.Headers.ContentType = new MediaTypeHeaderValue( "text/html" );
            return response;
        }
    }

    /// <summary>
    /// This is a class to contain a summary of the Transaction Details for the lava
    /// </summary>
    public class AccountSummary : DotLiquid.Drop
    {
        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the public name of the account.
        /// </summary>
        /// <value>
        /// The public name of the account.
        /// </value>
        public string PublicName { get; set; }

        /// <summary>
        /// Gets or sets the description of the account.
        /// </summary>
        /// <value>
        /// The description of the account.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        /// <value>
        /// The total.
        /// </value>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        public int Order { get; set; }
    }
}

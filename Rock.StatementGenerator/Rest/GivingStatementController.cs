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

namespace Rock.StatementGenerator.Rest
{
    public class GivingStatementController : ApiControllerBase
    {
        [System.Web.Http.Route( "api/GivingStatement/{personId}" )]
        [HttpGet]
        [Authenticate, Secured]
        public HttpResponseMessage Render( int personId, [FromUri] int? year = null, [FromUri] int? templateDefinedValueId = null, [FromUri] string additionalMergeObjects = null )
        {
            var definedTypeCache = DefinedTypeCache.Get( SystemGuid.DefinedType.STATEMENT_GENERATOR_LAVA_TEMPLATE );

            if ( definedTypeCache == null )
            {
                throw new Exception( "The defined type 'Statement Generator Lava Template' could not be found." );
            }

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

            var template = templateValue.GetAttributeValue( "LavaTemplate" );

            if ( string.IsNullOrEmpty( template ) )
            {
                throw new Exception( "The template cannot be null or empty" );
            }

            var lavaOptions = new CommonMergeFieldsOptions
            {
                GetPageContext = false,
                GetPageParameters = false,
                GetCurrentPerson = true,
                GetCampuses = true,
                GetLegacyGlobalMergeFields = false
            };

            var currentPerson = GetPerson();
            var mergeFields = LavaHelper.GetCommonMergeFields( null, currentPerson, lavaOptions );

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

            if ( !mergeFields.ContainsKey( "LavaTemplate" ) )
            {
                mergeFields.Add( "LavaTemplate", templateValue );
            }

            var body = template.ResolveMergeFields( mergeFields, currentPerson );
            var footer = templateValue.GetAttributeValue( "FooterHtml" ) ?? string.Empty;
            var html = string.Format( "{0}\n{1}", body, footer );
            //return new string( html.Where( c => !char.IsControl( c ) ).ToArray() );

            var response = new HttpResponseMessage();
            response.Content = new StringContent( html );
            response.Content.Headers.ContentType = new MediaTypeHeaderValue( "text/html" );
            return response;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Web.UI;
using Newtonsoft.Json;
using RestSharp;
using Rock.Attribute;
using Rock.Financial;
using Rock.Model;
using Rock.TransNational.Pi.Controls;
using Rock.Web.Cache;

// Use Newtonsoft RestRequest which is the same as RestSharp.RestRequest but uses the JSON.NET serializer
using RestRequest = RestSharp.Newtonsoft.Json.RestRequest;

namespace Rock.TransNational.Pi
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Rock.Financial.GatewayComponent" />
    [Description( "The TransNational Pi Gateway is the primary gateway to use with My Well giving." )]
    [DisplayName( "TransNational Pi Gateway" )]
    [Export( typeof( GatewayComponent ) )]
    [ExportMetadata( "ComponentName", "TransNational Pi Gateway" )]

    #region Component Attributes

    [TextField(
        "Private API Key",
        Key = AttributeKey.PrivateApiKey,
        Description = "The private API Key used for internal operations",
        Order = 1 )]

    [TextField(
        "Public API Key",
        Key = AttributeKey.PublicApiKey,
        Description = "The public API Key used for web client operations",
        Order = 2
        )]

    [TextField(
        "Gateway URL",
        Key = AttributeKey.GatewayUrl,
        Description = "The base URL of the gateway. For example: https://app.gotnpgateway.com for production or https://sandbox.gotnpgateway.com for testing",
        Order = 3
        )]

    #endregion Component Attributes
    public class PiGateway : GatewayComponent, IHostedGatewayComponent
    {
        #region Attribute Keys

        /// <summary>
        /// Keys to use for Component Attributes
        /// </summary>
        protected static class AttributeKey
        {
            public const string PrivateApiKey = "PrivateApiKey";
            public const string PublicApiKey = "PublicApiKey";
            public const string GatewayUrl = "GatewayUrl";
        }

        #endregion Attribute Keys

        /// <summary>
        /// Gets the gateway URL.
        /// </summary>
        /// <value>
        /// The gateway URL.
        /// </value>
        public string GetGatewayUrl( FinancialGateway financialGateway )
        {
            return this.GetAttributeValue( financialGateway, AttributeKey.GatewayUrl );
        }

        /// <summary>
        /// Gets the public API key.
        /// </summary>
        /// <value>
        /// The public API key.
        /// </value>
        public string GetPublicApiKey( FinancialGateway financialGateway )
        {
            return this.GetAttributeValue( financialGateway, AttributeKey.PublicApiKey );
        }

        /// <summary>
        /// Gets the private API key.
        /// </summary>
        /// <value>
        /// The private API key.
        /// </value>
        private string GetPrivateApiKey( FinancialGateway financialGateway )
        {
            return this.GetAttributeValue( financialGateway, AttributeKey.PrivateApiKey );
        }

        #region IHostedGatewayComponent

        /// <summary>
        /// Gets the hosted payment information control which will be used to collect CreditCard, ACH fields
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="enableACH">if set to <c>true</c> [enable ach]. (Credit Card is always enabled)</param>
        /// <param name="controlId">The control identifier.</param>
        /// <returns></returns>
        public Control GetHostedPaymentInfoControl( FinancialGateway financialGateway, bool enableACH, string controlId )
        {
            PiHostedPaymentControl piHostedPaymentControl = new PiHostedPaymentControl { ID = controlId };
            piHostedPaymentControl.PiGateway = this;
            piHostedPaymentControl.GatewayBaseUrl = this.GetGatewayUrl( financialGateway );
            if ( enableACH )
            {
                piHostedPaymentControl.EnabledPaymentTypes = new PiPaymentType[] { PiPaymentType.card, PiPaymentType.ach };
            }
            else
            {
                piHostedPaymentControl.EnabledPaymentTypes = new PiPaymentType[] { PiPaymentType.card };
            }

            piHostedPaymentControl.PublicApiKey = this.GetPublicApiKey( financialGateway );

            return piHostedPaymentControl;
        }

        /// <summary>
        /// Gets the paymentInfoToken that the hostedPaymentInfoControl returned (see also <seealso cref="M:Rock.Financial.IHostedGatewayComponent.GetHostedPaymentInfoControl(Rock.Model.FinancialGateway,System.String)" />)
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="hostedPaymentInfoControl">The hosted payment information control.</param>
        /// <returns></returns>
        public string GetHostedPaymentInfoToken( FinancialGateway financialGateway, Control hostedPaymentInfoControl )
        {
            var tokenReponse = ( hostedPaymentInfoControl as PiHostedPaymentControl ).PaymentInfoTokenRaw.FromJsonOrNull<Pi.BaseResponseData>();
            if ( tokenReponse?.IsSuccessStatus() != true )
            {
                throw new Exception( tokenReponse.Message );
            }
            else
            {
                return ( hostedPaymentInfoControl as PiHostedPaymentControl ).PaymentInfoToken;
            }
        }

        /// <summary>
        /// Gets the JavaScript needed to tell the hostedPaymentInfoControl to get send the paymentInfo and get a token
        /// Put this on your 'Next' or 'Submit' button so that the hostedPaymentInfoControl will fetch the token/response
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="hostedPaymentInfoControl">The hosted payment information control.</param>
        /// <returns></returns>
        public string GetHostPaymentInfoSubmitScript( FinancialGateway financialGateway, Control hostedPaymentInfoControl )
        {
            return $"submitTokenizer('{hostedPaymentInfoControl.ClientID}');";
        }

        /// <summary>
        /// Gets the URL that the Gateway Information UI will navigate to when they click the 'Learn More' link
        /// </summary>
        /// <value>
        /// The learn more URL.
        /// </value>
        public string LearnMoreURL => "https://www.mywell.org";

        /// <summary>
        /// Gets the URL that the Gateway Information UI will navigate to when they click the 'Configure' link
        /// </summary>
        /// <value>
        /// The configure URL.
        /// </value>
        public string ConfigureURL => "https://www.mywell.org/get-started/";

        #endregion IHostedGatewayComponent

        #region Temp Methods

        #region Customers

        /// <summary>
        /// Creates the customer.
        /// https://sandbox.gotnpgateway.com/docs/api/#create-a-new-customer
        /// NOTE: Pi Gateway supports multiple payment tokens per customer, but Rock will implement it as one Payment Method per Customer, and 0 or more Pi Customers per Rock Person.
        /// </summary>
        /// <param name="gatewayUrl">The gateway URL.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="tokenizerToken">The tokenizer token.</param>
        /// <param name="paymentInfo">The payment information.</param>
        /// <returns></returns>
        private CreateCustomerResponse CreateCustomer( string gatewayUrl, string apiKey, string tokenizerToken, PaymentInfo paymentInfo )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( "api/customer", Method.POST );
            restRequest.AddHeader( "Authorization", apiKey );

            var createCustomer = new CreateCustomerRequest
            {
                Description = paymentInfo.FullName,
                PaymentMethod = new PaymentMethodRequest( tokenizerToken ),
                BillingAddress = new BillingAddress
                {
                    FirstName = paymentInfo.FirstName,
                    LastName = paymentInfo.LastName,
                    AddressLine1 = paymentInfo.Street1,
                    AddressLine2 = paymentInfo.Street2,
                    City = paymentInfo.City,
                    State = paymentInfo.State,
                    PostalCode = paymentInfo.PostalCode,
                    Country = paymentInfo.Country,
                    Email = paymentInfo.Email,
                    Phone = paymentInfo.Phone,
                }
            };

            restRequest.AddJsonBody( createCustomer );

            ToggleAllowUnsafeHeaderParsing( true );

            var response = restClient.Execute( restRequest );

            var createCustomerResponse = JsonConvert.DeserializeObject<CreateCustomerResponse>( response.Content );
            return createCustomerResponse;
        }

        #endregion Customers

        #region Transactions

        /// <summary>
        /// Posts a transaction.
        /// https://sandbox.gotnpgateway.com/docs/api/#processing-a-transaction
        /// </summary>
        /// <param name="gatewayUrl">The gateway URL.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="type">The type (sale, authorize, credit)</param>
        /// <param name="referencedPaymentInfo">The referenced payment information.</param>
        /// <returns></returns>
        private CreateTransactionResponse PostTransaction( string gatewayUrl, string apiKey, TransactionType type, ReferencePaymentInfo referencedPaymentInfo )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( "api/transaction", Method.POST );
            restRequest.AddHeader( "Authorization", apiKey );

            var customerId = referencedPaymentInfo.GatewayPersonIdentifier;
            var tokenizerToken = referencedPaymentInfo.ReferenceNumber;
            var amount = referencedPaymentInfo.Amount;

            var transaction = new Rock.TransNational.Pi.CreateTransaction
            {
                Type = type,
                Amount = amount
            };

            if ( customerId.IsNotNullOrWhiteSpace() )
            {
                transaction.PaymentMethodRequest = new Rock.TransNational.Pi.PaymentMethodRequest( new Rock.TransNational.Pi.PaymentMethodCustomer( customerId ) );
            }
            else
            {
                transaction.PaymentMethodRequest = new Rock.TransNational.Pi.PaymentMethodRequest( tokenizerToken );
            }

            transaction.BillingAddress = new BillingAddress
            {
                FirstName = referencedPaymentInfo.FirstName,
                LastName = referencedPaymentInfo.LastName,
                AddressLine1 = referencedPaymentInfo.Street1,
                AddressLine2 = referencedPaymentInfo.Street2,
                City = referencedPaymentInfo.City,
                State = referencedPaymentInfo.State,
                PostalCode = referencedPaymentInfo.PostalCode,
                Country = referencedPaymentInfo.Country,
                Email = referencedPaymentInfo.Email,
                Phone = referencedPaymentInfo.Phone,
                CustomerId = customerId
            };

            restRequest.AddJsonBody( transaction );

            //ToggleAllowUnsafeHeaderParsing( true );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<CreateTransactionResponse>();
        }

        /// <summary>
        /// Gets the transaction status.
        /// </summary>
        /// <param name="gatewayUrl">The gateway URL.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <returns></returns>
        private TransactionStatusResponse GetTransactionStatus( string gatewayUrl, string apiKey, string transactionId )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/transaction/{transactionId}", Method.GET );
            restRequest.AddHeader( "Authorization", apiKey );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<TransactionStatusResponse>();
        }

        /// <summary>
        /// Posts the void.
        /// </summary>
        /// <param name="gatewayUrl">The gateway URL.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <returns></returns>
        private TransactionVoidRefundResponse PostVoid( string gatewayUrl, string apiKey, string transactionId )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/transaction/{transactionId}/void", Method.GET );
            restRequest.AddHeader( "Authorization", apiKey );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<TransactionVoidRefundResponse>();
        }

        /// <summary>
        /// Posts the refund.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <returns></returns>
        private TransactionVoidRefundResponse PostRefund( string gatewayUrl, string apiKey, string transactionId, decimal amount )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/transaction/{transactionId}/refund", Method.GET );
            restRequest.AddHeader( "Authorization", apiKey );

            var refundRequest = new TransactionRefundRequest { Amount = amount };
            restRequest.AddJsonBody( refundRequest );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<TransactionVoidRefundResponse>();
        }

        #endregion Transactions

        #region Plans

        /// <summary>
        /// Updates the billing plan BillingFrequency, BillingCycleInterval, BillingDays and Duration
        /// </summary>
        /// <param name="billingPlanParameters">The billing plan parameters.</param>
        /// <param name="scheduleTransactionFrequencyValueGuid">The schedule transaction frequency value unique identifier.</param>
        private static void SetBillingPlanParameters( BillingPlanParameters billingPlanParameters, Guid scheduleTransactionFrequencyValueGuid )
        {
            BillingFrequency? billingFrequency = null;
            int billingCycleInterval = 1;
            string billingDays = null;
            if ( scheduleTransactionFrequencyValueGuid == Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY.AsGuid() )
            {
                billingFrequency = BillingFrequency.monthly;
            }
            else if ( scheduleTransactionFrequencyValueGuid == Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY.AsGuid() )
            {
                // see https://sandbox.gotnpgateway.com/docs/api/#bill-once-month-on-the-1st-and-the-15th-until-canceled
                billingFrequency = BillingFrequency.twice_monthly;
                billingDays = "1,15";
            }
            else if ( scheduleTransactionFrequencyValueGuid == Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY.AsGuid() )
            {
                // see https://sandbox.gotnpgateway.com/docs/api/#bill-once-every-7-days-until-canceled
                billingCycleInterval = 1;
                billingFrequency = BillingFrequency.daily;
                billingDays = "7";
            }
            else if ( scheduleTransactionFrequencyValueGuid == Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY.AsGuid() )
            {
                // see https://sandbox.gotnpgateway.com/docs/api/#bill-once-other-week-until-canceled
                billingCycleInterval = 2;
                billingFrequency = BillingFrequency.daily;
                billingDays = "7";
            }

            billingPlanParameters.BillingFrequency = billingFrequency;
            billingPlanParameters.BillingCycleInterval = billingCycleInterval;
            billingPlanParameters.BillingDays = billingDays;
            billingPlanParameters.Duration = 0;
        }

        /// <summary>
        /// Creates the plan.
        /// https://sandbox.gotnpgateway.com/docs/api/#create-a-plan
        /// </summary>
        /// <param name="gatewayUrl">The gateway URL.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="planParameters">The plan parameters.</param>
        /// <returns></returns>
        private CreatePlanResponse CreatePlan( string gatewayUrl, string apiKey, CreatePlanParameters planParameters )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( "api/recurring/plan", Method.POST );
            restRequest.AddHeader( "Authorization", apiKey );

            restRequest.AddJsonBody( planParameters );
            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<CreatePlanResponse>();
        }

        /// <summary>
        /// Deletes the plan.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="planId">The plan identifier.</param>
        /// <returns></returns>
        private string DeletePlan( string gatewayUrl, string apiKey, string planId )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/recurring/plan/{planId}", Method.GET );
            restRequest.AddHeader( "Authorization", apiKey );
            var response = restClient.Execute( restRequest );

            return response.Content;
        }

        /// <summary>
        /// Gets the plans.
        /// https://sandbox.gotnpgateway.com/docs/api/#get-all-plans
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns></returns>
        private GetPlansResult GetPlans( string gatewayUrl, string apiKey )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( "api/recurring/plans", Method.GET );
            restRequest.AddHeader( "Authorization", apiKey );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<GetPlansResult>();
        }

        #endregion Plans

        #region Transaction Query

        /// <summary>
        /// Returns a list of Transactions that meet the queryTransactionStatusRequest parameters
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="queryTransactionStatusRequest">The query transaction status request.</param>
        /// <returns></returns>
        private TransactionSearchResult SearchTransactions( string gatewayUrl, string apiKey, QueryTransactionStatusRequest queryTransactionStatusRequest )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( "api/transaction/search", Method.POST );
            restRequest.AddHeader( "Authorization", apiKey );

            restRequest.AddJsonBody( queryTransactionStatusRequest );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<TransactionSearchResult>();
        }

        #endregion

        #region Subscriptions

        /// <summary>
        /// Creates the subscription.
        /// https://sandbox.gotnpgateway.com/docs/api/#create-a-subscription
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="subscriptionParameters">The subscription parameters.</param>
        /// <returns></returns>
        private SubscriptionResponse CreateSubscription( string gatewayUrl, string apiKey, SubscriptionRequestParameters subscriptionParameters )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( "api/recurring/subscription", Method.POST );
            restRequest.AddHeader( "Authorization", apiKey );

            restRequest.AddJsonBody( subscriptionParameters );
            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<SubscriptionResponse>();
        }

        /// <summary>
        /// Updates the subscription.
        /// https://sandbox.gotnpgateway.com/docs/api/#update-a-subscription
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="subscriptionParameters">The subscription parameters.</param>
        /// <returns></returns>
        private SubscriptionResponse UpdateSubscription( string gatewayUrl, string apiKey, string subscriptionId, SubscriptionRequestParameters subscriptionParameters )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/recurring/subscription/{subscriptionId}", Method.POST );
            restRequest.AddHeader( "Authorization", apiKey );

            restRequest.AddJsonBody( subscriptionParameters );
            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<SubscriptionResponse>();
        }

        /// <summary>
        /// Deletes the subscription.
        /// https://sandbox.gotnpgateway.com/docs/api/#delete-a-subscription
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="subscriptionParameters">The subscription parameters.</param>
        /// <returns></returns>
        private SubscriptionResponse DeleteSubscription( string gatewayUrl, string apiKey, string subscriptionId )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/recurring/subscription/{subscriptionId}", Method.DELETE );
            restRequest.AddHeader( "Authorization", apiKey );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<SubscriptionResponse>();
        }

        /// <summary>
        /// Gets the subscription.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <returns></returns>
        private SubscriptionResponse GetSubscription( string gatewayUrl, string apiKey, string subscriptionId )
        {
            var restClient = new RestClient( gatewayUrl );
            RestRequest restRequest = new RestRequest( $"api/recurring/subscription/{subscriptionId}", Method.GET );
            restRequest.AddHeader( "Authorization", apiKey );

            var response = restClient.Execute( restRequest );

            return response.Content.FromJsonOrNull<SubscriptionResponse>();
        }

        #endregion Subscriptions

        #region utility

        // Derived from http://o2platform.wordpress.com/2010/10/20/dealing-with-the-server-committed-a-protocol-violation-sectionresponsestatusline/
        public static bool ToggleAllowUnsafeHeaderParsing( bool enable )
        {
            //Get the assembly that contains the internal class
            Assembly aNetAssembly = Assembly.GetAssembly( typeof( System.Net.Configuration.SettingsSection ) );
            if ( aNetAssembly != null )
            {
                //Use the assembly in order to get the internal type for the internal class
                Type aSettingsType = aNetAssembly.GetType( "System.Net.Configuration.SettingsSectionInternal" );
                if ( aSettingsType != null )
                {
                    //Use the internal static property to get an instance of the internal settings class.
                    //If the static instance isn't created already the property will create it for us.
                    object anInstance = aSettingsType.InvokeMember( "Section",
                      BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[] { } );
                    if ( anInstance != null )
                    {
                        //Locate the private bool field that tells the framework is unsafe header parsing should be allowed or not
                        FieldInfo aUseUnsafeHeaderParsing = aSettingsType.GetField( "useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance );
                        if ( aUseUnsafeHeaderParsing != null )
                        {
                            aUseUnsafeHeaderParsing.SetValue( anInstance, true );
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        #endregion utility

        #endregion Temp Methods

        #region Exceptions

        /// <summary>
        /// 
        /// </summary>
        /// <seealso cref="System.Exception" />
        public class ReferencePaymentInfoRequired : Exception
        {
            public ReferencePaymentInfoRequired()
                : base( "PiGateway requires a token or customer reference" )
            {
            }
        }

        #endregion 

        #region GatewayComponent implementation

        /// <summary>
        /// Gets the supported payment schedules.
        /// </summary>
        /// <value>
        /// The supported payment schedules.
        /// </value>
        public override List<DefinedValueCache> SupportedPaymentSchedules
        {
            get
            {
                var values = new List<DefinedValueCache>();
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ) );

                // TODO enable these when Pi add these
                //values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY ) );
                //values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY ) );

                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY ) );
                values.Add( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY ) );
                return values;
            }
        }

        /// <summary>
        /// Charges the specified payment info.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        /// <exception cref="ReferencePaymentInfoRequired"></exception>
        public override FinancialTransaction Charge( FinancialGateway financialGateway, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            var referencedPaymentInfo = paymentInfo as ReferencePaymentInfo;
            if ( referencedPaymentInfo == null )
            {
                throw new ReferencePaymentInfoRequired();
            }



            var response = this.PostTransaction( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), TransactionType.sale, referencedPaymentInfo );
            if ( !response.IsSuccessStatus() )
            {
                errorMessage = response.Message;
                return null;
            }

            var financialTransaction = new FinancialTransaction();
            financialTransaction.TransactionCode = response.Data.Id;
            return financialTransaction;
        }

        /// <summary>
        /// Credits (Refunds) the specified transaction.
        /// </summary>
        /// <param name="origTransaction">The original transaction.</param>
        /// <param name="amount">The amount.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override FinancialTransaction Credit( FinancialTransaction origTransaction, decimal amount, string comment, out string errorMessage )
        {
            if ( origTransaction == null || origTransaction.TransactionCode.IsNullOrWhiteSpace() || origTransaction.FinancialGateway == null )
            {
                errorMessage = "Invalid original transaction, transaction code, or gateway.";
                return null;
            }

            var transactionId = origTransaction.TransactionCode;
            FinancialGateway financialGateway = origTransaction.FinancialGateway;

            var transactionStatus = this.GetTransactionStatus( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), transactionId );
            var transactionStatusTransaction = transactionStatus.Data.FirstOrDefault( a => a.Id == transactionId );
            TransactionVoidRefundResponse response;
            if ( transactionStatusTransaction.IsPendingSettlement() )
            {
                // https://sandbox.gotnpgateway.com/docs/api/#void
                response = this.PostVoid( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), transactionId );
            }
            else
            {
                // https://sandbox.gotnpgateway.com/docs/api/#refund
                response = this.PostRefund( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), transactionId, origTransaction.TotalAmount );
            }

            if ( response.IsSuccessStatus() )
            {
                var transaction = new FinancialTransaction();
                transaction.TransactionCode = "#TODO#";
                errorMessage = string.Empty;
                return transaction;
            }

            errorMessage = response.Message;
            return null;
        }

        /// <summary>
        /// Adds the scheduled payment.
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="schedule">The schedule.</param>
        /// <param name="paymentInfo">The payment info.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        /// <exception cref="ReferencePaymentInfoRequired"></exception>
        public override FinancialScheduledTransaction AddScheduledPayment( FinancialGateway financialGateway, PaymentSchedule schedule, PaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = string.Empty;
            var referencedPaymentInfo = paymentInfo as ReferencePaymentInfo;
            if ( referencedPaymentInfo == null )
            {
                throw new ReferencePaymentInfoRequired();
            }

            var customerId = referencedPaymentInfo.ReferenceNumber;

            // TODO: Create a new Plan for each subscription, or reuse existing ones??
            CreatePlanParameters createPlanParameters = new CreatePlanParameters
            {
                Name = schedule.TransactionFrequencyValue.Value,
                Description = $"Plan for PersonId: {schedule.PersonId }",
                Amount = paymentInfo.Amount
            };

            SetBillingPlanParameters( createPlanParameters, schedule.TransactionFrequencyValue.Guid );

            var planResponse = this.CreatePlan( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), createPlanParameters );

            var planId = planResponse.Data.Id;

            SubscriptionRequestParameters subscriptionParameters = new SubscriptionRequestParameters
            {
                Customer = new SubscriptionCustomer { Id = customerId },
                PlanId = planId,
                Description = $"Subscription for PersonId: {schedule.PersonId }",
                NextBillDate = schedule.StartDate,
                Duration = 0,
                Amount = paymentInfo.Amount
            };

            SetBillingPlanParameters( subscriptionParameters, schedule.TransactionFrequencyValue.Guid );

            var subscriptionResult = this.CreateSubscription( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), subscriptionParameters );
            var subscriptionId = subscriptionResult.Data?.Id;

            if ( subscriptionId.IsNullOrWhiteSpace() )
            {
                errorMessage = subscriptionResult.Message;
                return null;
            }

            var scheduledTransaction = new FinancialScheduledTransaction();
            scheduledTransaction.TransactionCode = subscriptionId;
            scheduledTransaction.GatewayScheduleId = subscriptionId;
            scheduledTransaction.FinancialGatewayId = financialGateway.Id;

            GetScheduledPaymentStatus( scheduledTransaction, out errorMessage );
            return scheduledTransaction;
        }

        /// <summary>
        /// Updates the scheduled payment.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="paymentInfo">The payment information.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool UpdateScheduledPayment( FinancialScheduledTransaction scheduledTransaction, PaymentInfo paymentInfo, out string errorMessage )
        {
            var subscriptionId = scheduledTransaction.GatewayScheduleId;

            SubscriptionRequestParameters subscriptionParameters = new SubscriptionRequestParameters
            {
                NextBillDate = scheduledTransaction.StartDate,
                Duration = 0,
                Amount = paymentInfo.Amount
            };

            SetBillingPlanParameters( subscriptionParameters, scheduledTransaction.TransactionFrequencyValue.Guid );

            FinancialGateway financialGateway = scheduledTransaction.FinancialGateway;

            var subscriptionResult = this.UpdateSubscription( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), subscriptionId, subscriptionParameters );
            if ( subscriptionResult.IsSuccessStatus() )
            {
                errorMessage = string.Empty;
                return true;
            }
            else
            {
                errorMessage = subscriptionResult.Message;
                return false;
            }
        }

        /// <summary>
        /// Cancels the scheduled payment.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool CancelScheduledPayment( FinancialScheduledTransaction scheduledTransaction, out string errorMessage )
        {
            var subscriptionId = scheduledTransaction.GatewayScheduleId;

            FinancialGateway financialGateway = scheduledTransaction.FinancialGateway;

            var subscriptionResult = this.DeleteSubscription( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), subscriptionId );
            if ( subscriptionResult.IsSuccessStatus() )
            {
                errorMessage = string.Empty;
                return true;
            }
            else
            {
                errorMessage = subscriptionResult.Message;
                return false;
            }
        }

        /// <summary>
        /// Flag indicating if gateway supports reactivating a scheduled payment.
        /// </summary>
        public override bool ReactivateScheduledPaymentSupported => false;

        /// <summary>
        /// Reactivates the scheduled payment.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool ReactivateScheduledPayment( FinancialScheduledTransaction scheduledTransaction, out string errorMessage )
        {
            errorMessage = "The payment gateway associated with this scheduled transaction (Pi) does not support reactivating scheduled transactions. A new scheduled transaction should be created instead.";
            return false;
        }

        /// <summary>
        /// Gets the scheduled payment status.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override bool GetScheduledPaymentStatus( FinancialScheduledTransaction scheduledTransaction, out string errorMessage )
        {
            var subscriptionId = scheduledTransaction.GatewayScheduleId;

            FinancialGateway financialGateway = scheduledTransaction.FinancialGateway;

            var subscriptionResult = this.GetSubscription( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), subscriptionId );
            if ( subscriptionResult.IsSuccessStatus() )
            {
                errorMessage = string.Empty;
                return true;
            }
            else
            {
                errorMessage = subscriptionResult.Message;
                return false;
            }
        }

        /// <summary>
        /// Gets the payments that have been processed for any scheduled transactions
        /// </summary>
        /// <param name="financialGateway">The financial gateway.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override List<Payment> GetPayments( FinancialGateway financialGateway, DateTime startDate, DateTime endDate, out string errorMessage )
        {
            QueryTransactionStatusRequest queryTransactionStatusRequest = new QueryTransactionStatusRequest
            {
                DateRange = new QueryDateRange( startDate, endDate )
            };

            var searchResult = this.SearchTransactions( this.GetGatewayUrl( financialGateway ), this.GetPrivateApiKey( financialGateway ), queryTransactionStatusRequest );

            if ( !searchResult.IsSuccessStatus() )
            {
                errorMessage = searchResult.Message;
                return null;
            }

            errorMessage = string.Empty;

            var paymentList = new List<Payment>();

            foreach ( var transaction in searchResult.Data )
            {
                var payment = new Payment
                {
                    AccountNumberMasked = transaction.PaymentMethodResponse.Card.MaskedCard,
                    Amount = transaction.Amount,
                    TransactionDateTime = transaction.CreatedDateTime.Value,

                    GatewayScheduleId = transaction.PaymentMethod

                };

                paymentList.Add( payment );
            }

            return paymentList;
        }

        /// <summary>
        /// Gets an optional reference number needed to process future transaction from saved account.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber( FinancialTransaction transaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            return transaction?.ScheduledTransaction.GatewayScheduleId?.ToString();
        }

        /// <summary>
        /// Gets an optional reference number needed to process future transaction from saved account.
        /// </summary>
        /// <param name="scheduledTransaction">The scheduled transaction.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override string GetReferenceNumber( FinancialScheduledTransaction scheduledTransaction, out string errorMessage )
        {
            errorMessage = string.Empty;
            return scheduledTransaction.GatewayScheduleId?.ToString();
        }


        #endregion GatewayComponent implementation
    }
}

using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace Rock.TransNational.Pi.Controls
{
    /// <summary>
    /// Control for hosting the Pi Gateway Payment Info HTML and scripts
    /// </summary>
    /// <seealso cref="System.Web.UI.WebControls.CompositeControl" />
    /// <seealso cref="System.Web.UI.INamingContainer" />
    public class PiHostedPaymentControl : CompositeControl, INamingContainer
    {
        #region Controls

        private HiddenFieldWithClass _hfPaymentInfoToken;
        private HiddenFieldWithClass _hfTokenizerRawResponse;
        private HiddenFieldWithClass _hfEnabledPaymentTypesJSON;
        private HiddenFieldWithClass _hfPublicApiKey;
        private HiddenFieldWithClass _hfGatewayUrl;
        private TextBox _hiddenInputStyleHook;
        private Panel _gatewayIFrameContainer;

        #endregion

        private PiGateway _piGateway;

        /// <summary>
        /// Gets or sets the gateway base URL.
        /// </summary>
        /// <value>
        /// The gateway base URL.
        /// </value>
        public string GatewayBaseUrl
        {
            get
            {
                EnsureChildControls();
                return _hfGatewayUrl.Value;
            }

            set
            {
                EnsureChildControls();
                _hfGatewayUrl.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the enabled payment types.
        /// </summary>
        /// <value>
        /// The enabled payment types.
        /// </value>
        public PiPaymentType[] EnabledPaymentTypes
        {
            set
            {
                EnsureChildControls();
                _hfEnabledPaymentTypesJSON.Value = value.Select( a => a.ConvertToString() ).ToJson();
            }
        }

        /// <summary>
        /// Sets the public API key.
        /// </summary>
        /// <value>
        /// The public API key.
        /// </value>
        public string PublicApiKey
        {
            set
            {
                EnsureChildControls();
                _hfPublicApiKey.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the pi gateway.
        /// </summary>
        /// <value>
        /// The pi gateway.
        /// </value>
        public PiGateway PiGateway
        {
            private get
            {
                return _piGateway;
            }

            set
            {
                _piGateway = value;
            }
        }

        /// <summary>
        /// Gets the payment information token.
        /// </summary>
        /// <value>
        /// The payment information token.
        /// </value>
        public string PaymentInfoToken
        {
            get
            {
                EnsureChildControls();
                return _hfPaymentInfoToken.Value;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            // Script that lets us use the Tokenizer API (see https://sandbox.gotnpgateway.com/docs/tokenizer/)
            RockPage.AddScriptSrcToHead( this.Page, "gotnpgatewayTokenizer", $"{GatewayBaseUrl}/tokenizer/tokenizer.js" );

            // Script that contains the initializeTokenizer scripts for us to use on the client
            System.Web.UI.ScriptManager.RegisterClientScriptBlock( this, this.GetType(), "piGatewayTokenizerBlock", Scripts.gatewayTokenizer, true );

            System.Web.UI.ScriptManager.RegisterStartupScript( this, this.GetType(), "piGatewayTokenizerStartup", $"initializeTokenizer('{this.ClientID}');", true );

            base.OnInit( e );
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            Controls.Clear();
            _hfPaymentInfoToken = new HiddenFieldWithClass() { ID = "_hfPaymentInfoToken", CssClass = "js-response-token" };
            Controls.Add( _hfPaymentInfoToken );

            _hfTokenizerRawResponse = new HiddenFieldWithClass() { ID = "_hfTokenizerRawResponse", CssClass = "js-tokenizer-raw-response" };
            Controls.Add( _hfTokenizerRawResponse );

            _hfEnabledPaymentTypesJSON = new HiddenFieldWithClass() { ID = "_hfEnabledPaymentTypesJSON", CssClass = "js-enabled-payment-types" };
            Controls.Add( _hfEnabledPaymentTypesJSON );

            _hfPublicApiKey = new HiddenFieldWithClass() { ID = "_hfPublicApiKey", CssClass = "js-public-api-key" };
            Controls.Add( _hfPublicApiKey );

            _hiddenInputStyleHook = new TextBox();
            _hiddenInputStyleHook.Attributes["class"] = "js-input-style-hook";
            _hiddenInputStyleHook.Style["display"] = "none";
            Controls.Add( _hiddenInputStyleHook );

            _hfGatewayUrl = new HiddenFieldWithClass() { ID = "_hfGatewayUrl", CssClass = "js-gateway-url" };
            Controls.Add( _hfGatewayUrl );

            _gatewayIFrameContainer = new Panel() { ID = "_gatewayIFrameContainer", CssClass = "js-gateway-iframe-container" };
            Controls.Add( _gatewayIFrameContainer );
        }
    }
}

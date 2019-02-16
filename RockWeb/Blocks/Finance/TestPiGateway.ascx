<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TestPiGateway.ascx.cs" Inherits="RockWeb.Blocks.Finance.TestPiGateway" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-star"></i>
                    Test Pi Gateway
                </h1>

            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-8">


                        <h1>Get Amount</h1>
                        <Rock:CampusAccountAmountPicker ID="caapAccountInfo" runat="server" AmountEntryMode="SingleAccount" />

                        <hr />

                        <h1>Hosted Payment Control</h1>
                        <div style="border-width: thick; border-color: red; border-style: solid;" class="margin-b-md">
                            <Rock:DynamicPlaceholder ID="phHostedPaymentControl" runat="server" />
                        </div>

                        <span class="btn btn-primary btn-sm" runat="server" id="btnSubmitPaymentInfo">Get Token</span>
                        <Rock:RockLiteral ID="lTokenOutput" runat="server" Label="Token Output" Text="-" />

                        <hr />
                        <Rock:NotificationBox ID="wbToken" runat="server" NotificationBoxType="Warning" Text="Note that Tokens can only be used once. (You'll get an 'Internal Server Error' if you use it more than once)." />

                        <Rock:PanelWidget ID="pwInternalGatewayFunctions" runat="server" Title="Internal Pi Gateway Functions" Visible="false">
                            <h1>Create Plan</h1>
                            <Rock:RockTextBox ID="tbPlanName" runat="server" Label="Plan Name" Text="test plan" />
                            <Rock:RockTextBox ID="tbPlanDescription" runat="server" Label="Plan Description" Text="test plan description" />
                            <Rock:NumberBox ID="tbPlanAmount" runat="server" Label="Plan Amount" Text="1.00" NumberType="Currency" />
                            <Rock:NumberBox ID="tbPlanBillingCycleInterval" runat="server" Label="Plan billing_cycle_interval" Help="How often to run the billing cycle. Run every x months" />
                            <Rock:RockDropDownList ID="ddlPlanBillingFrequency" runat="server" Label="Plan billing_frequency" Help="How often run within a billing cycle. (monthly, twice_monthly, ??)">
                                <asp:ListItem Text="monthly" />
                                <asp:ListItem Text="twice_monthly" />
                                <asp:ListItem Text="daily" />
                            </Rock:RockDropDownList>
                            <Rock:RockTextBox ID="tbPlanBillingDays" runat="server" Label="Plan billing_days" Help="Which day to bill on. If twice_monthly, then comma separate dates" />
                            <asp:LinkButton ID="btnCreatePlan" runat="server" CssClass="btn btn-primary" OnClick="btnCreatePlan_Click" Text="Create Plan" />
                            <Rock:RockTextBox ID="tbCreatePlanResponse_PlanID" runat="server" Label="Plan Id" />
                            <Rock:CodeEditor ID="ceCreatePlanResponse" runat="server" EditorMode="JavaScript" Label="Plan Response" EditorHeight="200" />


                            <h1>Get Plans</h1>
                            <asp:LinkButton ID="btnGetPlans" runat="server" CssClass="btn btn-primary" OnClick="btnGetPlans_Click" Text="Get Plans" />
                            <Rock:CodeEditor ID="ceGetPlansResponse" runat="server" EditorMode="JavaScript" Label="Plans Response" EditorHeight="400" />


                            <h1>Create Subscription</h1>
                            <Rock:RockTextBox ID="tbSubscriptionCustomerId" runat="server" Label="Customer.Id" Help="Customer ID to bill" />
                            <Rock:RockTextBox ID="tbSubscriptionDescription" runat="server" Label="Subscription Description" Text="test Subscription description" />
                            <Rock:RockTextBox ID="tbPlanId" runat="server" Label="Plan Id (Template)" Help="Plan ID to reference as a template" />

                            <Rock:NotificationBox ID="nbSubscriptionPlanOverrides" runat="server" Text="Leave Amount, billing_cycle_interval, billing_frequency and/or billing_days blank to use Plan defaults" />
                            <Rock:NumberBox ID="tbSubscriptionAmount" runat="server" Label="Subscription Amount" Text="2.00" NumberType="Currency" />
                            <Rock:NumberBox ID="tbSubscriptionBillingCycleInterval" runat="server" Label="Subscription billing_cycle_interval" Help="How often to run the billing cycle. Run every x months, or every x (days + billing days) to create weekly, biweekly" />
                            <Rock:RockDropDownList ID="ddlSubscriptionBillingFrequency" runat="server" Label="Subscription billing_frequency" Help="How often run within a billing cycle. (monthly, twice_monthly, ??)">
                                <asp:ListItem Text="(use plan default)" />
                                <asp:ListItem Text="monthly" />
                                <asp:ListItem Text="twice_monthly" />
                                <asp:ListItem Text="daily" />
                            </Rock:RockDropDownList>
                            <Rock:RockTextBox ID="tbSubscriptionBillingDays" runat="server" Label="Subscription billing_days" Help="Which day to bill on. If twice_monthly, then comma separate dates" />
                            <Rock:NumberBox ID="nbSubscriptionDuration" runat="server" Label="Subscription duration" Help="(No Documention)" />
                            <Rock:RockTextBox ID="tbSubscriptionNextBillDate" runat="server" Label="Subscription next_bill_date" Help="(No Documention). Appears to be first date of the recurring payment and what the recurring schedule it based upon in YYYY-MM-DD format." Text="2019-3-1" />
                            <asp:LinkButton ID="btnCreateSubscription" runat="server" CssClass="btn btn-primary" OnClick="btnCreateSubscription_Click" Text="Create Subscription" />
                            <Rock:RockTextBox ID="tbCreateSubscriptionResponse_SubscriptionId" runat="server" Label="Subscription Id" />
                            <Rock:CodeEditor ID="ceCreateSubscriptionResponse" runat="server" EditorMode="JavaScript" Label="Create Subscription Response" EditorHeight="400" />

                            <h1>Get Customer Transaction Statuses</h1>
                            <asp:LinkButton ID="btnGetCustomerTransactionStatus" runat="server" CssClass="btn btn-primary" OnClick="btnGetCustomerTransactionStatus_Click" Text="Query Transactions" />
                            <Rock:CodeEditor ID="ceQueryTransactionStatus" runat="server" EditorMode="JavaScript" Label="CustomerTransactionStatus Response" EditorHeight="400" />
                        </Rock:PanelWidget>


                        <h1>Customer (Billing Information)</h1>
                        <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                        <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                        <Rock:AddressControl ID="acAddress" runat="server" UseStateAbbreviation="true" UseCountryAbbreviation="false" Label="Address" />
                        <Rock:PhoneNumberBox ID="pnbPhone" runat="server" Label="Phone" />
                        <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" />

                        <h1>Process One-Time Sale</h1>
                        <Rock:NotificationBox ID="nbAmountRequired" runat="server" NotificationBoxType="Danger" Text="Amount is required" Visible="false" />
                        <asp:LinkButton ID="btnProcessSale" runat="server" CssClass="btn btn-primary" Text="Process Sale" OnClick="btnProcessSale_Click" />
                        <Rock:CodeEditor ID="ceSaleResponse" runat="server" EditorMode="JavaScript" Label="Sale Response" EditorHeight="400" />
                    </div>
                    <div class="col-md-4">
                        <h2>Developer API</h2>
                        <p>
                            <a href="https://sandbox.gotnpgateway.com/docs/api">Pi Developer Docs (api)</a>
                        </p>
                        <h2>Test Cards</h2>
                        <table class="grid-table table table-bordered table-striped table-hover">
                            <thead>
                                <tr>
                                    <th>Card Number</th>
                                    <th>Card Brand</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td>4111111111111111</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4005519200000004</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4009348888881881</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4012000033330026</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4012000077777777</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4012888888881881</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4217651111111119</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>4500600000000061</td>
                                    <td>visa</td>
                                </tr>
                                <tr>
                                    <td>5555555555554444</td>
                                    <td>mastercard</td>
                                </tr>
                                <tr>
                                    <td>2223000048400011</td>
                                    <td>mastercard</td>
                                </tr>
                                <tr>
                                    <td>378282246310005</td>
                                    <td>amex</td>
                                </tr>
                                <tr>
                                    <td>371449635398431</td>
                                    <td>amex</td>
                                </tr>
                                <tr>
                                    <td>6011111111111117</td>
                                    <td>discover</td>
                                </tr>
                                <tr>
                                    <td>36259600000004</td>
                                    <td>diners</td>
                                </tr>
                                <tr>
                                    <td>3530111333300000</td>
                                    <td>jcb</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

            </div>

        </asp:Panel>

        <script type="text/javascript">
            function displayToken() {
                var token = $('.js-response-token').val();
                $('.js-token-output').val(token);
            };
        </script>

    </ContentTemplate>
</asp:UpdatePanel>

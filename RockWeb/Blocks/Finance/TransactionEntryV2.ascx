<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TransactionEntryV2.ascx.cs" Inherits="RockWeb.Blocks.Finance.TransactionEntryV2" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <%-- Message for any configuration warnings --%>
        <Rock:NotificationBox ID="nbConfigurationNotification" runat="server" Visible="false" />

        <Rock:NotificationBox ID="nbInvalidPersonWarning" runat="server" Visible="false" />


        <%-- Friendly Help if there is no Gateway configured --%>
        <asp:Panel ID="pnlGatewayHelp" runat="server" Visible="false">
            <h4>Welcome to Rock's On-line Giving Experience</h4>
            <p>There is currently no gateway configured. Below are a list of gateways installed on your server. You can also add additional gateways through the Rock Shop.</p>
            <asp:Repeater ID="rptInstalledGateways" runat="server" OnItemDataBound="rptInstalledGateways_ItemDataBound">
                <ItemTemplate>
                    <div class="panel panel-block">
                        <div class="panel-body">
                            <asp:HiddenField ID="hfGatewayEntityTypeId" runat="server" />
                            <h4>
                                <asp:Literal ID="lGatewayName" runat="server" /></h4>
                            <p>
                                <asp:Literal ID="lGatewayDescription" runat="server" />
                            </p>
                            <div class="actions">
                                <asp:HyperLink ID="aGatewayConfigure" runat="server" CssClass="btn btn-xs btn-success" Text="Configure" />
                                <asp:HyperLink ID="aGatewayLearnMore" runat="server" CssClass="btn btn-xs btn-link" Text="Learn More" />
                            </div>
                        </div>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:Panel>

        <asp:Panel ID="pnlTransactionEntry" runat="server">
            <asp:HiddenField ID="hfTargetPersonId" runat="server" />

            <div class="row">
                <%-- Scheduled Gifts Panel --%>
                <asp:Panel ID="pnlScheduledTransactions" runat="server" CssClass="col-sm-4 scheduled-transactions" Visible="false">
                    <h4>
                        <asp:Literal ID="lScheduledTransactionsTitle" runat="server" Text="Scheduled Transactions" /></h4>

                    <asp:Repeater ID="rptScheduledTransactions" runat="server" OnItemDataBound="rptScheduledTransactions_ItemDataBound">
                        <ItemTemplate>
                            <div class="scheduled-transaction js-scheduled-transaction">

                                <asp:HiddenField ID="hfScheduledTransactionId" runat="server" />
                                <Rock:HiddenFieldWithClass ID="hfScheduledTransactionExpanded" CssClass="js-scheduled-transaction-expanded" runat="server" Value="false" />

                                <div class="panel panel-default">
                                    <div class="panel-heading">
                                        <span class="panel-title h1"><i class="fa fa-calendar"></i>
                                            <asp:Literal ID="lScheduledTransactionTitle" runat="server" /></span>

                                        <span class="js-scheduled-totalamount scheduled-totalamount margin-l-md">
                                            <asp:Literal ID="lScheduledTransactionAmountTotal" runat="server" />
                                        </span>

                                        <div class="panel-actions pull-right">
                                            <span class="js-toggle-scheduled-details toggle-scheduled-details clickable fa fa-plus"></span>
                                        </div>
                                    </div>
                                    <div class="js-scheduled-details scheduled-details margin-l-lg">
                                        <div class="panel-body">

                                            <asp:Repeater ID="rptScheduledTransactionAccounts" runat="server" OnItemDataBound="rptScheduledTransactionAccounts_ItemDataBound">
                                                <ItemTemplate>
                                                    <div class="account-details margin-l-sm">
                                                        <span class="scheduled-transaction-account control-label">
                                                            <asp:Literal ID="lScheduledTransactionAccountName" runat="server" /></span>
                                                        <br />
                                                        <span class="scheduled-transaction-amount">
                                                            <asp:Literal ID="lScheduledTransactionAmount" runat="server" /></span>
                                                    </div>
                                                </ItemTemplate>
                                            </asp:Repeater>

                                            <Rock:NotificationBox ID="nbScheduledTransactionMessage" runat="server" Visible="false" />

                                            <asp:Panel ID="pnlActions" runat="server" CssClass="scheduled-details-actions">
                                                <asp:LinkButton ID="btnScheduledTransactionEdit" runat="server" CssClass="btn btn-sm btn-link" Text="Edit" OnClick="btnScheduledTransactionEdit_Click" />
                                                <asp:LinkButton ID="btnScheduledTransactionDelete" runat="server" CssClass="btn btn-sm btn-link" Text="Delete" OnClick="btnScheduledTransactionDelete_Click" />
                                            </asp:Panel>

                                        </div>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>

                <%-- Transaction Entry Panel --%>
                <div class="col-sm-8">
                    <h2>
                        <asp:Literal ID="lIntroMessage" runat="server" /></h2>

                    <%-- Collect Transaction Info --%>
                    <asp:Panel ID="pnlPromptForAmounts" runat="server">

                        <Rock:CampusAccountAmountPicker ID="caapPromptForAccountAmounts" runat="server" />

                        <asp:Panel ID="pnlScheduledTransaction" runat="server">

                            <Rock:ButtonDropDownList ID="ddlFrequency" runat="server" FormGroupCssClass=" margin-t-md" AutoPostBack="true" OnSelectionChanged="ddlFrequency_SelectionChanged" />

                            <div class="margin-t-md">
                                <Rock:ButtonDropDownList ID="ddlPersonSavedAccount" runat="server" Label="Giving Method" />
                                <Rock:DatePicker ID="dtpStartDate" runat="server" Label="First Gift" AllowPastDateSelection="false" />
                            </div>

                        </asp:Panel>
                    </asp:Panel>

                    <%-- Collect/Update Personal Information --%>
                    <asp:Panel ID="pnlPersonalInformation" runat="server" Visible="false">
                    </asp:Panel>

                    <%-- Collect Payment Info --%>
                    <asp:Panel ID="pnlPaymentInfo" runat="server" Visible="true">
                        <h1>##Hosted Payment Control##</h1>
                        <div style="border-width: thick; border-color: red; border-style: solid;" class="margin-b-md">
                            <Rock:DynamicPlaceholder ID="phHostedPaymentControl" runat="server" />
                        </div>

                        <asp:LinkButton ID="btnGetPaymentInfoNext" runat="server" Text="Next" CssClass="btn btn-primary js-submit-hostedpaymentinfo" OnClick="btnGetPaymentInfoNext_Click" />
                    </asp:Panel>

                </div>
            </div>

        </asp:Panel>

        <script type="text/javascript">

            // Scheduled Transaction Javascripts
            function setScheduledDetailsVisibility($container, animate) {
                var $scheduledDetails = $container.find('.js-scheduled-details');
                var $expanded = $container.find('.js-scheduled-transaction-expanded');
                var $totalAmount = $container.find('.js-scheduled-totalamount');
                var $toggle = $container.find('.js-toggle-scheduled-details');

                if ($expanded.val() == 1) {
                    if (animate) {
                        $scheduledDetails.slideDown();
                        $totalAmount.fadeOut();
                    } else {
                        $scheduledDetails.show();
                        $totalAmount.hide();
                    }

                    $toggle.removeClass('fa-plus').addClass('fa-minus');
                } else {
                    if (animate) {
                        $scheduledDetails.slideUp();
                        $totalAmount.fadeIn();
                    } else {
                        $scheduledDetails.hide();
                        $totalAmount.show();
                    }

                    $toggle.removeClass('fa-minus').addClass('fa-plus');
                }
            };

            Sys.Application.add_load(function () {
                var $scheduleDetailsContainers = $('.js-scheduled-transaction');

                $scheduleDetailsContainers.each(function (index) {
                    setScheduledDetailsVisibility($($scheduleDetailsContainers[index]), false);
                });

                $('.js-submit-hostedpaymentinfo').click(function () {
                    debugger
                    <%=HostPaymentInfoSubmitScript%>
                });


                var $toggleScheduledDetails = $('.js-toggle-scheduled-details');
                $toggleScheduledDetails.click(function () {
                    var $scheduledDetailsContainer = $(this).closest('.js-scheduled-transaction');
                    var $expanded = $scheduledDetailsContainer.find('.js-scheduled-transaction-expanded');
                    if ($expanded.val() == 1) {
                        $expanded.val(0);
                    } else {
                        $expanded.val(1);
                    }

                    setScheduledDetailsVisibility($scheduledDetailsContainer, true);
                });
            });
        </script>


    </ContentTemplate>
</asp:UpdatePanel>

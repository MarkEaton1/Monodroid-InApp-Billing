using System.Runtime.CompilerServices;

// Copyright 2010 Google Inc. All Rights Reserved.

using Android.App;
using Android.Content;
using Android.Util;
using Java.Lang;

namespace Billing
{
    /// <summary>
    /// This class contains the methods that handle responses from Android Market.  The
    /// implementation of these methods is specific to a particular application.
    /// The methods in this example update the database and, if the main application
    /// has registered a {@llink PurchaseObserver}, will also update the UI.  An
    /// application might also want to forward some responses on to its own server,
    /// and that could be done here (in a background thread) but this example does
    /// not do that.
    /// 
    /// You should modify and obfuscate this code before using it.
    /// </summary>
    public class ResponseHandler
    {
        private const string TAG = "ResponseHandler";

        /// <summary>
        /// This is a static instance of <seealso cref="PurchaseObserver"/> that the
        /// application creates and registers with this class. The PurchaseObserver
        /// is used for updating the UI if the UI is visible.
        /// </summary>
        private static PurchaseObserver sPurchaseObserver;

        /// <summary>
        /// Registers an observer that updates the UI. </summary>
        /// <param name="observer"> the observer to register </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Register(PurchaseObserver observer)
        {
            sPurchaseObserver = observer;
        }

        /// <summary>
        /// Unregisters a previously registered observer. </summary>
        /// <param name="observer"> the previously registered observer. </param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Unregister(PurchaseObserver observer)
        {
            sPurchaseObserver = null;
        }

        /// <summary>
        /// Notifies the application of the availability of the MarketBillingService.
        /// This method is called in response to the application calling
        /// <seealso cref="BillingService#checkBillingSupported()"/>. </summary>
        /// <param name="supported"> true if in-app billing is supported. </param>
        public static void CheckBillingSupportedResponse(bool supported, string type)
        {
            if (sPurchaseObserver != null)
            {
                sPurchaseObserver.OnBillingSupported(supported, type);
            }
        }

        /// <summary>
        /// Starts a new activity for the user to buy an item for sale. This method
        /// forwards the intent on to the PurchaseObserver (if it exists) because
        /// we need to start the activity on the activity stack of the application.
        /// </summary>
        /// <param name="pendingIntent"> a PendingIntent that we received from Android Market that
        ///     will create the new buy page activity </param>
        /// <param name="intent"> an intent containing a request id in an extra field that
        ///     will be passed to the buy page activity when it is created </param>
        public static void BuyPageIntentResponse(PendingIntent pendingIntent, Intent intent)
        {
            if (sPurchaseObserver == null)
            {
                if (Consts.DEBUG)
                {
                    Log.Debug(TAG, "UI is not running");
                }
                return;
            }
            sPurchaseObserver.StartBuyPageActivity(pendingIntent, intent);
        }

        /// <summary>
        /// Notifies the application of purchase state changes. The application
        /// can offer an item for sale to the user via
        /// <seealso cref="BillingService#requestPurchase(String)"/>. The BillingService
        /// calls this method after it gets the response. Another way this method
        /// can be called is if the user bought something on another device running
        /// this same app. Then Android Market notifies the other devices that
        /// the user has purchased an item, in which case the BillingService will
        /// also call this method. Finally, this method can be called if the item
        /// was refunded. </summary>
        /// <param name="purchaseState"> the state of the purchase request (PURCHASED,
        ///     CANCELED, or REFUNDED) </param>
        /// <param name="productId"> a string identifying a product for sale </param>
        /// <param name="orderId"> a string identifying the order </param>
        /// <param name="purchaseTime"> the time the product was purchased, in milliseconds
        ///     since the epoch (Jan 1, 1970) </param>
        /// <param name="developerPayload"> the developer provided "payload" associated with
        ///     the order </param>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: public static void purchaseResponse(final android.content.Context context, final com.example.dungeons.Consts.PurchaseState purchaseState, final String productId, final String orderId, final long purchaseTime, final String developerPayload)
        public static void PurchaseResponse(Context context, Consts.PurchaseState purchaseState, string productId, string orderId, long purchaseTime, string developerPayload)
        {

            // Update the database with the purchase state. We shouldn't do that
            // from the main thread so we do the work in a background thread.
            // We don't update the UI here. We will update the UI after we update
            // the database because we need to read and update the current quantity
            // first.
            //JAVA TO C# CONVERTER TODO TASK: Anonymous inner classes are not converted to C# if the base type is not defined in the code being converted:
            new Thread(new Runnable(() =>
                {
                    PurchaseDatabase db = new PurchaseDatabase(context);
                    int quantity = db.UpdatePurchase(orderId, productId, purchaseState, purchaseTime, developerPayload);
                    db.Close();

                    // This needs to be synchronized because the UI thread can change the
                    // value of sPurchaseObserver.
                    lock (context)
                    {
                        if (sPurchaseObserver != null)
                        {
                            sPurchaseObserver.PostPurchaseStateChange(purchaseState, productId, quantity, purchaseTime, developerPayload);
                        }
                    }
                })).Start();
        }

        /// <summary>
        /// This is called when we receive a response code from Android Market for a
        /// RequestPurchase request that we made.  This is used for reporting various
        /// errors and also for acknowledging that an order was sent successfully to
        /// the server. This is NOT used for any purchase state changes. All
        /// purchase state changes are received in the <seealso cref="BillingReceiver"/> and
        /// are handled in <seealso cref="Security#verifyPurchase(String, String)"/>. </summary>
        /// <param name="context"> the context </param>
        /// <param name="request"> the RequestPurchase request for which we received a
        ///     response code </param>
        /// <param name="responseCode"> a response code from Market to indicate the state
        /// of the request </param>
        public static void ResponseCodeReceived(Context context, BillingService.RequestPurchase request, Consts.ResponseCode responseCode)
        {
            if (sPurchaseObserver != null)
            {
                sPurchaseObserver.OnRequestPurchaseResponse(request, responseCode);
            }
        }

        /// <summary>
        /// This is called when we receive a response code from Android Market for a
        /// RestoreTransactions request. </summary>
        /// <param name="context"> the context </param>
        /// <param name="request"> the RestoreTransactions request for which we received a
        ///     response code </param>
        /// <param name="responseCode"> a response code from Market to indicate the state
        ///     of the request </param>
        public static void ResponseCodeReceived(Context context, BillingService.RestoreTransactions request, Consts.ResponseCode responseCode)
        {
            if (sPurchaseObserver != null)
            {
                sPurchaseObserver.OnRestoreTransactionsResponse(request, responseCode);
            }
        }
    }

}
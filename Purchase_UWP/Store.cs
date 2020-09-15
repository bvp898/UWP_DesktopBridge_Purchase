using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Services.Store;
using System;
using Windows.ApplicationModel.Core;

namespace UWP
{
    //https://techcommunity.microsoft.com/t5/windows-dev-appconsult/enable-in-app-product-purchases-for-desktop-bridge-converted/ba-p/316901


    //Declare the IInitializeWithWindow interface in your app's code with the ComImport attribute
    [ComImport]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }


    public static class Store
    {
        private static StoreContext context = null;
        private static StoreProduct subscriptionStoreProduct = null;
        private static int userOwnsSubscription = -1; //-1 connecting  0 unsubscription 1 subscripted
        // Assign this variable to the Store ID of your subscription add-on.
        private static string subscriptionStoreId = "9P28P93430J0";

        public static int IsUserOwnsSubscription()
        {
            return userOwnsSubscription;
        }
     


        static async Task GetAppInfoAsync()
        {
         
            StoreProductResult storeProduct = await context.GetStoreProductForCurrentAppAsync();

            System.Diagnostics.Debug.WriteLine("GetAppInfoAsync price " + storeProduct.Product.Price);  
        }

        public async static Task SetupSubscriptionInfoAsync(/*OnSubscriptionChanged callback*/)
        {
            System.Diagnostics.Debug.WriteLine("SetupSubscriptionInfoAsync dotNet");
            //mOnSubscriptionChangedCallback = callback;
            if (context == null)
            {
                context = StoreContext.GetDefault();

              
            }

   
            userOwnsSubscription = await CheckIfUserHasSubscriptionAsync();
            if (userOwnsSubscription == 1)
            {
                // Unlock all the subscription add-on features here.

                return;
            }

            // Get the StoreProduct that represents the subscription add-on.
            subscriptionStoreProduct = await GetSubscriptionProductAsync();
            if (subscriptionStoreProduct == null)
            {
                return;
            }
            userOwnsSubscription = 0;

            // Prompt the customer to purchase the subscription.
            await PromptUserToPurchaseAsync();
        }

        private async static Task<int> CheckIfUserHasSubscriptionAsync()
        {
            StoreAppLicense appLicense = await context.GetAppLicenseAsync();

            // Check if the customer has the rights to the subscription.
            foreach (var addOnLicense in appLicense.AddOnLicenses)
            {
                StoreLicense license = addOnLicense.Value;
                if (license.SkuStoreId.StartsWith(subscriptionStoreId))
                {
                    if (license.IsActive)
                    {
                        // The expiration date is available in the license.ExpirationDate property.
                        return 1;
                    }
                }
            }

            // The customer does not have a license to the subscription.
            return -1; //still need GetSubscriptionProductAsync
        }

        private async static Task<StoreProduct> GetSubscriptionProductAsync()
        {
            // Load the sellable add-ons for this app and check if the trial is still 
            // available for this customer. If they previously acquired a trial they won't 
            // be able to get a trial again, and the StoreProduct.Skus property will 
            // only contain one SKU.

            StoreProductQueryResult result =
                await context.GetAssociatedStoreProductsAsync(new string[] { "Durable" });

            if (result.ExtendedError != null)
            {
                System.Diagnostics.Debug.WriteLine("Something went wrong while getting the add-ons. " +
                    "ExtendedError:" + result.ExtendedError);
                return null;
            }

            // Look for the product that represents the subscription.
            foreach (var item in result.Products)
            {
                StoreProduct product = item.Value;

                System.Diagnostics.Debug.WriteLine("GetSubscriptionProductAsync product " + product.StoreId);

                if (product.StoreId == subscriptionStoreId)
                {
                    return product;
                }
            }
             
            System.Diagnostics.Debug.WriteLine("The subscription was not found.");
            return null;
        }


        //buy
      
        public async static Task PromptUserToPurchaseAsync()
        {

            // If your app is a desktop app that uses the Desktop Bridge, you
            // may need additional code to configure the StoreContext object.
            // For more info, see https://aka.ms/storecontext-for-desktop.

            System.Diagnostics.Debug.WriteLine("PromptUserToPurchaseAsync dotNet context " + context);

            IInitializeWithWindow initWindow = (IInitializeWithWindow)(object)context;

            IntPtr handle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            System.Diagnostics.Debug.WriteLine("PromptUserToPurchaseAsync dotNet handle " + handle);

            initWindow.Initialize(handle);



            // Request a purchase of the subscription product. If a trial is available it will be offered 
            // to the customer. Otherwise, the non-trial SKU will be offered.
            StorePurchaseResult result = await subscriptionStoreProduct.RequestPurchaseAsync();

            // Capture the error message for the operation, if any.
            string extendedError = string.Empty;
            if (result.ExtendedError != null)
            {
                extendedError = result.ExtendedError.Message;
            }

            switch (result.Status)
            {
                case StorePurchaseStatus.Succeeded:
                    // Show a UI to acknowledge that the customer has purchased your subscription 
                    // and unlock the features of the subscription. 
                    userOwnsSubscription = 1;
                    break;

                case StorePurchaseStatus.NotPurchased:
                    System.Diagnostics.Debug.WriteLine("The purchase did not complete. " +
                        "The customer may have cancelled the purchase. ExtendedError: " + extendedError);
                    break;

                case StorePurchaseStatus.ServerError:
                case StorePurchaseStatus.NetworkError:
                    System.Diagnostics.Debug.WriteLine("The purchase was unsuccessful due to a server or network error. " +
                        "ExtendedError: " + extendedError);
                    break;

                case StorePurchaseStatus.AlreadyPurchased:
                    System.Diagnostics.Debug.WriteLine("The customer already owns this subscription." +
                            "ExtendedError: " + extendedError);
                    break;
            }

     

          
        }
    }
}

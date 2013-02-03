Monodroid-InApp-Billing
=======================

C# classes needed to use in-app billing for Monodroid

This has been designed for Android 2.0 and up. Has been fully tested for managed products with real money in the following scenarios:

- billing supported for managed products
- billing supported for subscriptions
- purchased
- cancelled
- restore transactions
- refund
- item unavailable
- database updated 

Although the code is all there it hasn't been tested for the following scenarios:

- 2 managed purchases quickly
- subscriptions
- unmanaged products

Please study the Android official documentation for testing in-app billing. Particularly with test accounts as there is some setup instructions you will need to follow in that documentation.

Also I have used Json.Net as part of the in-app billing (dll included).

As I work fulltime, all my Monodroid work is part time in my own hours. As such this code is not elegant. Even this demo, while works, could be a lot more user friendly.

Please feel free as a community to make it a more user friendly demo.

And be sure to check the wiki as I have added a troubleshooting guide.

Mark Eaton

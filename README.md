# cs-EVE-Assets-Value-Calculator
Determines the net worth of all the EVE Online characters associated with user-provided API keys.

This is a console-based desktop app that requires no additional user input beyond its initial setup, but in order for it to work, it does need to stay running. Frequency of data requests is once per hour from both CCP and EVE-Central's servers.

As of the current version, this program does require some setup before it works as intended. First, the user must place the .exe into a folder of choice. Within that folder, the user must create a eve_apikeys.txt file with an API key of a minimum level of permissions. The permissions must be Account Balance, AssetsList, MarketOrders, and Contracts. A full-access API key will work also, but expose the user unnecessarily should the key fall into the wrong hands. API Keys are generated for the user by an official CCP server. Please google that if you need more info. You do not have to have a subscribed account to generate an API key, but you do need to at least have an account. The template for API key info to be stored in the eve_apikeys.txt file is:

keyid1,verificationcode1
keyid2,verificationcode2

... with each additional key on a new line after the first.

This program also appends data into an external .CSV file, named eve_networth.csv, in a way that allows it to easily be used as a Data Source in an Excel spreadsheet. The user may find it handy to create this file on their own in the same directory as the .exe with the heading of "TIME,ACCOUNT NAME" to identify column names for the data. For each additional account after the first, type ",ACCOUNT NAME".

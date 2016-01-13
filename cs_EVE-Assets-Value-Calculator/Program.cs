using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Threading;
using System.Net;
using System.IO;

namespace cs_EVE_Assets_Value_Calculator
{
    class Program
    {
        int _totalcharacters = 0;
        Controller _eveaccounts = new Controller();

        string _baseurl = "https://api.eveonline.com";
        
        // accounts endpoint
        string _accountendpoint = "/account/Characters.xml.aspx";
        List<XmlDocument> _accountendpointresponses = new List<XmlDocument>();

        // assets endpoint
        string _assetsendpoint = "/char/AssetList.xml.aspx";
        List<XmlDocument> _assets = new List<XmlDocument>();
        SortedDictionary<string, int> _uniqueassets = new SortedDictionary<string, int>();

        // account balance endpoint
        string _accountbalanceendpoint = "/char/AccountBalance.xml.aspx";
        List<XmlDocument> _accountbalanceresponses = new List<XmlDocument>();

        // contracts endpoint
        string _contractsendpoint = "/char/Contracts.xml.aspx";
        string _contractitemsendpoint = "/char/ContractItems.xml.aspx";
        List<XmlDocument> _contractheaders = new List<XmlDocument>();
        List<XmlDocument> _contractitems = new List<XmlDocument>();
        Dictionary<string, string> _qualifyingheaders = new Dictionary<string, string>();

        // market endpoint
        string _marketordersendpoint = "/char/MarketOrders.xml.aspx";
        List<XmlDocument> _marketorderendpointresponses = new List<XmlDocument>();

        // eve-central api
        string _evecentralmarketstat = "http://api.eve-central.com/api/marketstat?usesystem=30000142&typeid=";
        List<XmlDocument> _evecentralresponses = new List<XmlDocument>();
        SortedDictionary<string, decimal> _uniqueitemvalues = new SortedDictionary<string, decimal>();

        public volatile static bool _continue = true;
        public static DateTime _lastattempt;


        static void Main(string[] args)
        {
            Console.Title = "EVE Networth";
            Console.WindowWidth = 30;
            Console.WindowHeight = 3;
            Start();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60 * 60 * 1000; // ~1 hours
            timer.Elapsed += timer_Elapsed;
            timer.Start();

            while (_continue) Thread.Sleep(1);
        }

        static void Start()
        {
            _lastattempt = DateTime.Now;
            Thread t = new Thread(new Program().Begin);
            t.IsBackground = true;
            t.Start();
            t.Join();

            Console.Clear();
            Console.WriteLine("Data retrieved.\nNext pull at {0}", _lastattempt.AddHours(1).ToLongTimeString());
        }

        public void Begin()
        {
            HardcodeAccountInfo();
            GetAccountXML();
            GetTotalCharacters();
            GetCharacterAssets();
            GetAccountBalances();
            GetMarketOrders();
            GetContractHeaders();
            GetContractContents();
            
            GetUniqueTypeIDsCount();
            GetItemValuesFromEVECentral();
            ProcessEVECentralMarketStatResponses();
            DisseminateJitaBuyMaxValues();
            WriteToCSV();
        }

        /***********************************************************************
         * 
         * RETRIEVAL FUNCTIONS
         * 
         ***********************************************************************/

        private void GetAccountXML()
        {
            Display("Retrieving characters.");
            StringBuilder sb = new StringBuilder();
            CCPAPIInterfacer[] w = new CCPAPIInterfacer[3];

            try
            {
                for (int i = 0; i < _eveaccounts.Count; ++i)
                {
                    w[i] = new CCPAPIInterfacer(_eveaccounts[i]);
                    w[i].DownloadStringCompleted += w_AccountEndpointResponse;
                    sb.Append(_baseurl).Append(_accountendpoint).Append("?keyid=").Append(_eveaccounts[i].KeyID)
                    .Append("&vcode=").Append(_eveaccounts[i].VCode);
                    w[i].DownloadStringAsync(new Uri(sb.ToString()));
                    sb.Clear();
                }
            }
            catch (Exception) { }

            while (_accountendpointresponses.Count != _eveaccounts.Count) Thread.Sleep(1);
        }

        private void GetCharacterAssets()
        {
            Display("Retrieving character assets.");
            StringBuilder sb = new StringBuilder();
            CCPAPIInterfacer[] w = new CCPAPIInterfacer[_totalcharacters];
            // build URL list
            // make requests
            int count = 0;
            try
            {
                foreach (Account a in _eveaccounts)
                {
                    foreach (Character c in a)
                    {
                        w[count] = new CCPAPIInterfacer(c);
                        sb.Append(_baseurl).Append(_assetsendpoint)
                        .Append("?keyid=").Append(a.KeyID)
                        .Append("&vcode=").Append(a.VCode)
                        .Append("&characterid=").Append(c.CharacterID);
                        w[count].DownloadStringCompleted += w_AssetListEndpointResponse;
                        w[count].DownloadStringAsync(new Uri(sb.ToString()));
                        sb.Clear();
                        ++count;
                    }
                }
            }
            catch (Exception) { }

            while (_assets.Count != _totalcharacters) Thread.Sleep(1);
        }

        private void GetAccountBalances()
        {
            Display("Retrieving account balances.");
            StringBuilder sb = new StringBuilder();
            CCPAPIInterfacer[] w = new CCPAPIInterfacer[_totalcharacters];
            int count = 0;
            try
            {
                foreach (Account a in _eveaccounts)
                {
                    foreach (Character c in a)
                    {
                        w[count] = new CCPAPIInterfacer(c);
                        sb.Append(_baseurl).Append(_accountbalanceendpoint)
                        .Append("?keyid=").Append(a.KeyID)
                        .Append("&vcode=").Append(a.VCode)
                        .Append("&characterid=").Append(c.CharacterID);
                        w[count].AssociatedCharacter = c;
                        w[count].DownloadStringCompleted += w_AccountBalanceEndpointResponse;
                        w[count].DownloadStringAsync(new Uri(sb.ToString()));
                        sb.Clear();
                        ++count;
                    }
                }
            }
            catch (Exception) { }

            while (_accountbalanceresponses.Count != _totalcharacters) Thread.Sleep(1);
        }

        private void GetMarketOrders()
        {
            Display("Retrieving market orders.");
            StringBuilder sb = new StringBuilder();
            CCPAPIInterfacer[] w = new CCPAPIInterfacer[_totalcharacters];
            // build URL list
            // make requests
            int count = 0;
            try
            {
                foreach (Account a in _eveaccounts)
                {
                    foreach (Character c in a)
                    {
                        w[count] = new CCPAPIInterfacer(c);
                        sb.Append(_baseurl).Append(_marketordersendpoint)
                        .Append("?keyid=").Append(a.KeyID)
                        .Append("&vcode=").Append(a.VCode)
                        .Append("&characterid=").Append(c.CharacterID);
                        w[count].AssociatedCharacter = c;
                        w[count].DownloadStringCompleted += w_MarketOrderEndpointResponse;
                        w[count].DownloadStringAsync(new Uri(sb.ToString()));
                        sb.Clear();
                        ++count;
                    }
                }
            }
            catch (Exception) { }

            while (_marketorderendpointresponses.Count != _totalcharacters) Thread.Sleep(1);
        }

        private void GetContractHeaders()
        {
            Display("Retrieving contract headers.");
            StringBuilder sb = new StringBuilder();
            CCPAPIInterfacer[] w = new CCPAPIInterfacer[_totalcharacters];

            int count = 0;
            try
            {
                foreach (Account a in _eveaccounts)
                {
                    foreach (Character c in a)
                    {
                        w[count] = new CCPAPIInterfacer(c);
                        sb.Append(_baseurl).Append(_contractsendpoint)
                            .Append("?keyid=").Append(a.KeyID)
                            .Append("&vcode=").Append(a.VCode)
                            .Append("&characterid=").Append(c.CharacterID);
                        w[count].AssociatedCharacter = c;
                        w[count].DownloadStringCompleted += w_ContractsEndpointResponse;
                        w[count].DownloadStringAsync(new Uri(sb.ToString()));
                        sb.Clear();
                        ++count;
                    }
                }
            }
            catch (Exception) { }

            while (_contractheaders.Count != _totalcharacters) Thread.Sleep(1);
        }

        private void GetContractContents()
        {
            Display("Retrieving contract items.");
            int requests = _qualifyingheaders.Count;
            try
            {
                if (requests > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    CCPAPIInterfacer[] w = new CCPAPIInterfacer[requests];

                    int count = 0;
                    foreach (var item in _qualifyingheaders)
                    {
                        foreach (Account a in _eveaccounts)
                        {
                            foreach (Character c in a)
                            {
                                if (item.Key.Equals(c.CharacterID))
                                {
                                    w[count] = new CCPAPIInterfacer(c);
                                    sb.Append(_baseurl).Append(_contractitemsendpoint)
                                        .Append("?keyid=").Append(a.KeyID)
                                        .Append("&vcode=").Append(a.VCode)
                                        .Append("&characterid=").Append(c.CharacterID)
                                        .Append("&contractid=").Append(item.Value);
                                    w[count].AssociatedCharacter = c;
                                    w[count].DownloadStringCompleted += w_ContractItemsEndpointResponse;
                                    w[count].DownloadStringAsync(new Uri(sb.ToString()));
                                    sb.Clear();
                                    ++count;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            while (_contractitems.Count != requests) Thread.Sleep(1);
        }

        private void GetItemValuesFromEVECentral()
        {
            List<string> uris = AssembleURIs();
            Display("Retrieving price data from EVE-Central.");
            int requests = uris.Count;
            System.Net.WebClient[] w = new System.Net.WebClient[requests];

            try
            {
                for (int i = 0; i < requests; ++i)
                {
                    w[i] = new System.Net.WebClient();
                    w[i].Proxy = null;
                    w[i].DownloadStringCompleted += w_EVECentralResponse;
                    w[i].Headers.Add("Program", "EVE-Asset-Value-v1");
                    w[i].Headers.Add("Contact", "garrett.bates@outlook.com");
                    w[i].DownloadStringAsync(new Uri(uris[i]));
                }
            }
            catch (Exception) { }

            while (_evecentralresponses.Count != requests) Thread.Sleep(1);
        }

        /***********************************************************************
         * 
         * CALLBACK FUNCTIONS
         * 
         ***********************************************************************/

        static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Start();
        }

        private void w_AccountEndpointResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                XmlDocument x = new XmlDocument();
                x.LoadXml(e.Result);
                lock (new object())
                {
                    AddXMLToCollection(x, _accountendpointresponses);
                    foreach (XmlNode node in x.SelectNodes("/eveapi/result/rowset/row"))
                    {
                        string name = node.Attributes[0].Value;
                        string charid = node.Attributes[1].Value;
                        ((CCPAPIInterfacer)(sender)).AssociatedAccount.Add(new Character(name, charid));
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void w_AssetListEndpointResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                XmlDocument x = new XmlDocument();
                x.LoadXml(e.Result);

                AddXMLToCollection(x, _assets);
                ((CCPAPIInterfacer)(sender)).AssociatedCharacter.AssetsXML = x;
            }
            catch (Exception)
            {

            }
        }

        private void w_AccountBalanceEndpointResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(e.Result);
                decimal balance = Convert.ToDecimal(xml.SelectSingleNode("/eveapi/result/rowset/row").Attributes["balance"].Value);
                ((CCPAPIInterfacer)(sender)).AssociatedCharacter.ISK = balance;
                AddXMLToCollection(xml, _accountbalanceresponses);
            }
            catch (Exception)
            {

            }
        }

        private void w_MarketOrderEndpointResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(e.Result);
                AddXMLToCollection(xmldoc, _marketorderendpointresponses);
                ((CCPAPIInterfacer)(sender)).AssociatedCharacter.MarketOrdersXML = xmldoc;
            }
            catch (Exception)
            {

            }
        }

        private void w_ContractsEndpointResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            XmlDocument xmldoc = new XmlDocument();

            try
            {
                xmldoc.LoadXml(e.Result);
                AddXMLToCollection(xmldoc, _contractheaders);
                DateTime d;
                foreach (XmlNode n in xmldoc.SelectNodes("/eveapi/result/rowset/row"))
                {
                    if (DateTime.TryParse(n.Attributes["dateCompleted"].Value, out d) == false)
                    {
                        lock (new object())
                        {
                            _qualifyingheaders.Add(n.Attributes["issuerID"].Value, n.Attributes["contractID"].Value);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void w_ContractItemsEndpointResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            XmlDocument xmldoc = new XmlDocument();

            try
            {
                xmldoc.LoadXml(e.Result);
                AddXMLToCollection(xmldoc, _contractitems);
                ((CCPAPIInterfacer)(sender)).AssociatedCharacter.ContractItemsXML = xmldoc;
            }
            catch (Exception) { }
        }

        private void w_EVECentralResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            XmlDocument xmldoc = new XmlDocument();
            try
            {
                xmldoc.LoadXml(e.Result);
                AddXMLToCollection(xmldoc, _evecentralresponses);
            }
            catch (Exception)
            {

            }
        }

        /***********************************************************************
         * 
         * SUPPORT FUNCTIONS
         * 
         ***********************************************************************/

        private void Display(string text)
        {
            Console.Clear();
            Console.WriteLine(text);
        }

        private void HardcodeAccountInfo()
        {
            using (StreamReader r = new StreamReader("eve_apikeys.txt"))
            {
                string[] lines = r.ReadToEnd().Split('\r');
                
                foreach (string line in lines)
                {
                    string keyid = line.Split(',')[0];
                    string vcode = line.Split(',')[1];
                    _eveaccounts.Add(new Account(keyid, vcode));
                }

                r.Close();
            }
        }

        private void GetTotalCharacters()
        {
            foreach (Account a in _eveaccounts)
            {
                foreach (Character c in a)
                {
                    ++_totalcharacters;
                }
            }
        }

        private List<string> AssembleURIs()
        {
            Display("Assembing URIs.");
            List<string> uris = new List<string>();
            StringBuilder sb = new StringBuilder();
            int maxurilength = 1500;
            string[] uniquetypeids = _uniqueassets.Keys.ToArray();

            sb.Append(_evecentralmarketstat);

            foreach (string typeid in uniquetypeids)
            {
                sb.Append(typeid).Append(',');

                if (sb.Length > maxurilength)
                {
                    if (sb[sb.Length - 1].Equals(','))
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }
                    uris.Add(sb.ToString());
                    sb.Clear();
                    sb.Append(_evecentralmarketstat);
                }
            }

            if (sb[sb.Length - 1].Equals(','))
            {
                sb.Remove(sb.Length - 1, 1);
            }
            uris.Add(sb.ToString());
            sb.Clear();

            return uris;
        }

        private void ClearVariables()
        {
            _eveaccounts.Clear();
            _accountendpointresponses.Clear();
            _assets.Clear();
            _uniqueassets.Clear();
            _accountbalanceresponses.Clear();
            _contractheaders.Clear();
            _contractitems.Clear();
            _qualifyingheaders.Clear();
            _marketorderendpointresponses.Clear();
            _evecentralresponses.Clear();
            _uniqueitemvalues.Clear();
        }

        private void WriteToCSV()
        {
            Display("Writing to CSV.");
            StringBuilder sb = new StringBuilder();
            decimal totalvalue = 0M;

            sb.Append(DateTime.UtcNow.ToShortDateString())
                .Append(" ").Append(DateTime.UtcNow.ToLongTimeString()).Append(',');

            foreach (Account a in _eveaccounts)
            {
                decimal accountvalue = 0M;

                foreach (Character c in a)
                {
                    decimal charactervalue = c.ISK + c.AssetsValue;
                    accountvalue += charactervalue;
                }

                sb.Append(accountvalue.ToString()).Append(',');
                totalvalue += accountvalue;
            }

            sb.Append(totalvalue.ToString());

            using (StreamWriter w = new StreamWriter("eve_networth.csv", true))
            {
                w.WriteLine(sb.ToString());
                w.Close();
            }
        }

        private void DisseminateJitaBuyMaxValues()
        {
            Display("Applying item values to characters.");
            // on a per-character basis
            foreach (Account a in _eveaccounts)
            {
                foreach (Character c in a)
                {
                    c.ProcessItemValues(_uniqueitemvalues);
                }
            }
        }

        private void ProcessEVECentralMarketStatResponses()
        {
            Display("Parsing item values.");
            foreach (XmlDocument d in _evecentralresponses)
            {
                foreach (XmlNode n in d.SelectNodes("/evec_api/marketstat/type"))
                {
                    string typeid = n.Attributes["id"].Value;
                    decimal buymax = Convert.ToDecimal(n.SelectSingleNode("buy/max").InnerText);

                    _uniqueitemvalues.Add(typeid, buymax);
                }
            }
        }

        private void FillDictionary(XmlNode n, SortedDictionary<string, int> items)
        {
            string typeid = n.Attributes["typeID"].Value;
            int quantity = Convert.ToInt32(n.Attributes["quantity"].Value);

            if (items.ContainsKey(typeid))
            {
                items[typeid] += quantity;
            }
            else
            {
                items.Add(typeid, quantity);
            }

            foreach (XmlNode subnode in n.SelectNodes("rowset/row"))
            {
                FillDictionary(subnode, items);
            }
        }

        private void GetUniqueTypeIDsCount()
        {
            Display("Creating list of unique typeIDs.");
            // assets
            foreach (XmlDocument x in _assets)
            {
                foreach (XmlNode n in x.SelectNodes("/eveapi/result/rowset/row"))
                {
                    FillDictionary(n, _uniqueassets);
                }
            }

            // contract items
            foreach (XmlDocument x in _contractitems)
            {
                foreach (XmlNode n in x.SelectNodes("/eveapi/result/rowset/row"))
                {
                    FillDictionary(n, _uniqueassets);
                }
            }

            // market orders
            foreach (XmlDocument d in _marketorderendpointresponses)
            {
                foreach (XmlNode n in d.SelectNodes("/eveapi/result/rowset/row"))
                {
                    if (n.Attributes["bid"].Value.Equals("0")) // sell orders
                    {
                        string typeid = n.Attributes["typeID"].Value;
                        string volremaining = n.Attributes["volRemaining"].Value;

                        if (_uniqueassets.ContainsKey(typeid))
                        {
                            _uniqueassets[typeid] += Convert.ToInt32(volremaining);
                        }
                        else
                        {
                            _uniqueassets.Add(typeid, Convert.ToInt32(volremaining));
                        }
                    }
                }
            }
        }

        private void AddXMLToCollection(XmlDocument xmldoc, List<XmlDocument> list)
        {
            lock (new object())
            {
                list.Add(xmldoc);
            }
        }

    }
}

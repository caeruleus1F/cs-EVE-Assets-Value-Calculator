using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace cs_EVE_Assets_Value_Calculator
{
    class Character
    {
        SortedDictionary<string, int> _assetsandcount = new SortedDictionary<string, int>();
        decimal _assetsvalue = 0M;
        decimal _isk = 0M;
        string _name = null;
        string _charid = null;

        public Character(string name, string charid)
        {
            _name = name;
            _charid = charid;
        }

        public decimal AssetsValue
        {
            get { return _assetsvalue; }
        }

        public XmlDocument AssetsXML
        {
            set 
            {
                foreach (XmlNode n in value.SelectNodes("/eveapi/result/rowset/row"))
                {
                    FillDictionary(n, _assetsandcount);
                }
            }
        }

        public XmlDocument ContractItemsXML
        {
            set 
            { 
                foreach (XmlNode n in value.SelectNodes("/eveapi/result/rowset/row"))
                {
                    FillDictionary(n, _assetsandcount);
                }
            }
        }

        public XmlDocument MarketOrdersXML
        {
            set 
            {
                foreach (XmlNode n in value.SelectNodes("/eveapi/result/rowset/row"))
                {
                    if (n.Attributes["orderState"].Value.Equals("0"))
                    {
                        if (n.Attributes["bid"].Value.Equals("0")) // sell orders
                        {
                            string typeid = n.Attributes["typeID"].Value;
                            string volremaining = n.Attributes["volRemaining"].Value;

                            if (_assetsandcount.ContainsKey(typeid))
                            {
                                _assetsandcount[typeid] += Convert.ToInt32(volremaining);
                            }
                            else
                            {
                                _assetsandcount.Add(typeid, Convert.ToInt32(volremaining));
                            }
                        }
                        else // buy orders
                        {
                            decimal isk = Convert.ToDecimal(n.Attributes["escrow"].Value);
                            _isk += isk;
                        }
                    }
                }
            }
        }

        public string CharacterID 
        { 
            get { return _charid; } 
            set { _charid = value; } 
        }

        public decimal ISK
        {
            get { return _isk; }
            set { _isk = value; }
        }

        public void ProcessItemValues(SortedDictionary<string, decimal> jitavalue)
        {
            foreach (var item in _assetsandcount)
            {
                _assetsvalue += item.Value * jitavalue[item.Key];
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
    }
}

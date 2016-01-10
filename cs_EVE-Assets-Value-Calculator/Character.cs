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
        XmlDocument _assetsxml = null;
        XmlDocument _contractitemsxml = null;
        XmlDocument _marketordersxml = null;
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
                _assetsxml = value;

                foreach (XmlNode n in _assetsxml.SelectNodes("/eveapi/result/rowset/row"))
                {
                    FillDictionary(n, _assetsandcount);
                }
            }
        }

        public XmlDocument ContractItemsXML
        {
            set 
            { 
                _contractitemsxml = value;

                foreach (XmlNode n in _contractitemsxml.SelectNodes("/eveapi/result/rowset/row"))
                {
                    FillDictionary(n, _assetsandcount);
                }
            }
        }

        public XmlDocument MarketOrdersXML
        {
            set 
            { 
                _marketordersxml = value;

                foreach (XmlNode n in _marketordersxml.SelectNodes("/eveapi/result/rowset/row"))
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

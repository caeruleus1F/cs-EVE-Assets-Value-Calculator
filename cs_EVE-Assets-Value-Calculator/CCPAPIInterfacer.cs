using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace cs_EVE_Assets_Value_Calculator
{
    class CCPAPIInterfacer : WebClient
    {
        Account _acct = null;
        Character _chr = null;

        public CCPAPIInterfacer()
        {
            Initialize();
        }

        public CCPAPIInterfacer(Account acct)
        {
            _acct = acct;
            Initialize();
        }

        public CCPAPIInterfacer(Character chr)
        {
            _chr = chr;
            Initialize();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.AutomaticDecompression = DecompressionMethods.GZip
                     | DecompressionMethods.Deflate;
            return request;
        }

        private void Initialize()
        {
            this.Proxy = null;
            this.Headers.Add("User-Agent", "EVE-Asset-Value-Calculator");
            this.Headers.Add("Contact", "garrett.bates@outlook.com");
            this.Headers.Add("IGN", "Thirtyone Organism");
            this.Headers.Add("Accept-Encoding", "gzip");
        }

        public Account AssociatedAccount
        {
            get { return _acct; }
            set { _acct = value; }
        }

        public Character AssociatedCharacter
        {
            get { return _chr; }
            set { _chr = value; }
        }
    }
}

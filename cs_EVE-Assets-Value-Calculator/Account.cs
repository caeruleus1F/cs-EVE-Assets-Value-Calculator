using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cs_EVE_Assets_Value_Calculator
{
    class Account : List<Character>
    {
        string _keyid = null;
        string _vcode = null;

        public Account()
        {

        }

        public Account(string keyid, string vcode)
        {
            _keyid = keyid;
            _vcode = vcode;
        }

        public string KeyID 
        { 
            get { return _keyid; } 
            set { _keyid = value; } 
        }

        public string VCode 
        { 
            get { return _vcode; } 
            set { _vcode = value; } 
        }


    }
}

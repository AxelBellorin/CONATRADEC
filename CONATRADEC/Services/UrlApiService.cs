using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CONATRADEC.Services
{
    public class UrlApiService
    {
        private const string baseUrlApi = "https://conatradecnic.azurewebsites.net/";
        public UrlApiService()
        {
        }

        public string BaseUrlApi { get => baseUrlApi; }
    }
}

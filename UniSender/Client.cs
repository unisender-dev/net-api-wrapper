using System;
using System.Dynamic;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.IO;
using System.Collections.Generic;

///Short manual of using UniSender's library
///First of all that library support Net 4.0 and higher
///for creation instatnce of UniSender.Client class type
///     dynamic client = new Client("You api key", "site language - optional");//default locale is 'en'
///than you access to all api requests that listed on http://unisender.com.ua/help/api
///For example, for getting collection of lists type:
///     var answer = client.getLists();
///if everything is right you can access to collection in that way
///     answer["result"][number_of_list]
///For passing parametrs uses instance of IDictionary<string,object>
///Single value will convert to "key=value" string, before encode value to url purpose
///Array vallue will convert to "key=item1,item2,item3...", also before encode every item
///Dictonary<string,object> value will convert to "key[subkey1]=value1,key[subkey2]=value2,key[subkey3]=value3"

namespace UniSender
{
    public class Client : DynamicObject 
    {
        public string ApiKey { get; set; }

        public string Lang { get; set; }

        public Client(string apiKey, string lang="en")
        {
            ApiKey = apiKey;
            Lang = lang;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = call_action(binder.Name, args);
            return true;
        }

        protected Dictionary<string, dynamic> call_action(string action, object[] args)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(get_url(action, args));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());
            var jss = new JavaScriptSerializer();
            return jss.Deserialize<dynamic>(streamReader.ReadToEnd());
        }

        public string get_url(string action, object[] args)
        {
            var url = "https://api.unisender.com/"+Lang+"/api/"+ action + "?format=json&api_key=" + ApiKey;
            if (args != null && args.Length != 0)
                url+= "&" + generate_query(args[0]);
            return url;
        }

        protected string generate_query(object args)
        {
            var querry = new StringBuilder();
            if (args is IDictionary<string, object>)
            {
                foreach (var pair in (IDictionary<string, object>)args)
                {
                    if (pair.Value is Array)
                    {
                        var array = pair.Value as Array;
                        var value = "";
                        foreach (var item in array)
                        {
                            value += HttpUtility.UrlEncode(item.ToString()) + ",";
                        }
                        if (value.Length > 1)
                            querry.Append(string.Format("{0}={1}&", pair.Key, value.Substring(0, value.Length-1)));
                    }
                    else if (pair.Value is IDictionary<string, object>)
                    {
                        var hash = pair.Value as IDictionary<string, object>;
                        var value = "";
                        foreach (var sub_pair in hash)
                        {
                            value += string.Format("{0}[{1}]={2}&", pair.Key, sub_pair.Key, HttpUtility.UrlEncode(sub_pair.Value.ToString()));
                        }
                        if (value.Length > 0)
                            querry.Append(value);
                    }
                    else
                    {
                        querry.Append(string.Format("{0}={1}&", pair.Key, HttpUtility.UrlEncode(pair.Value.ToString())));
                    }
                }
            }
            return querry.ToString();
        }


    }
}

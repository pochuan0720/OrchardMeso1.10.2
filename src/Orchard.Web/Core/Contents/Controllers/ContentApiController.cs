using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Orchard.Core.Contents.Controllers
{
    public class ContentApiController : ApiController
    {
        // GET: api/ContentsApi
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/ContentsApi/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/ContentsApi
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/ContentsApi/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/ContentsApi/5
        public void Delete(int id)
        {
        }
    }
}

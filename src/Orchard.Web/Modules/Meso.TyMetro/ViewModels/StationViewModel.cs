using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TYMetro.Management.Api.Models.Sites;

namespace Meso.TyMetro.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class StationViewModel
    {
        [JsonProperty]
        public string Id { get; set; }
        [JsonProperty]
        public string Code { get; set; }
        [JsonProperty]
        public string Title { get; set; }
        [JsonProperty]
        public int Seq { get; set; }


        public string Line { get; set; } = "A";

        //other for query
        public string culture { get; set; }

        public StationViewModel()
        {

        }

        public StationViewModel(SitesDataModel d, string culture)
        {
            Id = d.SiteID;
            Code = d.SiteCode;
            if (!string.IsNullOrEmpty(culture) && culture.Equals("en-US"))
                Title = d.SiteName_en;
            else
                Title = d.SiteName;

            //Lang = d.Language;
            if (d.Seq != null)
                Seq = (int)d.Seq;
            Line = d.line;

        }

        public SitesQueryModel ToQueryModel()
        {
            return new SitesQueryModel
            {
                SiteID = Id,
                SiteCode = Code,
                line = Line
            };
        }
    }


}
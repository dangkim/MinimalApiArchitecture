using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalApiArchitecture.Application.Model
{    public class ResponseTokenData
    {
        public string? Access_token { get; set; }
        public string? Token_type { get; set; }
        public long? Expires_in { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.miniteknoloji {
    class CrcNotMatchException:Exception {

        public CrcNotMatchException(): base("Incoming CRC does not match with the calculated") {
            
        }
    }
        
}

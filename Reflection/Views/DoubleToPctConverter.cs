using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Reflection.Views {
    public class DoubleToPctConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string result = "";
            double d = (double)value;
            result = string.Format("{0}%", Math.Round(d,0));

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
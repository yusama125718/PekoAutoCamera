using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PekoAutoCamera
{
    // 105Bankさんのカメラ用コンバータ
    class _105BankConverter
    {
        static double min_x = -34.2;
        static double max_x = 55.0;
        static double min_y = 0;
        static double max_y = 15.0;
        static double min_z = -34.2;
        static double max_z = 34.2;
        static double min_horizontal = 0;
        static double max_horizontal = 360;
        static double min_vertical = -90;
        static double max_vertical = 90;
        static double min_fov = 0;
        static double max_fov = 120;

        public static double ConvertX(double base_pos)
        {
            double range = max_x - min_x;
            double diff = base_pos - min_x;
            double osc_val = diff / range;

            osc_val = CorrectionValue(osc_val);
            return osc_val;
        }

        public static double ConvertY(double base_pos)
        {
            double range = max_y - min_y;
            double diff = base_pos - min_y;
            double osc_val = diff / range;

            osc_val = CorrectionValue(osc_val);
            return osc_val;
        }

        public static double ConvertZ(double base_pos)
        {
            double range = max_z - min_z;
            double diff = base_pos - min_z;
            double osc_val = diff / range;

            osc_val = CorrectionValue(osc_val);
            return osc_val;
        }

        public static double ConvertHorizontal(double base_rote)
        {
            double rote = (base_rote + 360) % 360;
            double range = max_horizontal - min_horizontal;
            double diff = rote - min_horizontal;
            double osc_val = diff / range;

            osc_val = CorrectionValue(osc_val);
            return osc_val;
        }

        public static double ConvertVertical(double base_rote)
        {
            double range = max_vertical - min_vertical;
            double diff = base_rote - min_vertical;
            double osc_val = diff / range;

            osc_val = CorrectionValue(osc_val);
            return osc_val;
        }

        public static double ConvertFov(double base_fov)
        {
            double range = max_fov - min_fov;
            double diff = base_fov - min_fov;
            double osc_val = diff / range;

            osc_val = CorrectionValue(osc_val);
            return osc_val;
        }

        private static double CorrectionValue(double value)
        {
            // 値の範囲外の場合補正する
            if (value > 1) value = 1;
            else if (value < 0) value = 0;

            return value;
        }
    }
}

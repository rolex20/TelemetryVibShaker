using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarThunderExporter
{
    internal class SimpleStatsCalculator
    {       
        int _samples, _sum, _min, _max;
        bool first;

        public void AddSample(int sample)
        {


            bool processSample = true;

            // ignores the first max value
            if (sample > _max)
                if (first)
                {
                    _max = sample;
                }
                else
                {
                    first = true;
                    processSample = false;
                }

            if (processSample)
            {
                _sum += sample;

                _samples++;

                if (sample < _min) _min = sample;
            }


        }

        public void Initialize()
        {
            _samples = 0;
            _sum = 0;
            _min = int.MaxValue;
            _max = 0;
            first = false;
        }

        public float Average() { 
            return (_samples==0)? 0.0f: (float)_sum / (float)_samples; 
        }

        public int Min() { return _min; }

        public int Max() { return _max; }

        public int SampleSize() { return _samples;}

        public SimpleStatsCalculator() { Initialize(); }
    

    }
}

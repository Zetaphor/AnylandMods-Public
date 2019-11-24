using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AvatarScriptBackend {
    class ContinuousFFT : MonoBehaviour {
        private Vector3[] poslog;
        private FFT2 fft;
        
        public double[] FFTXR { get; private set; }
        public double[] FFTXI { get; private set; }
        public double[] FFTYR { get; private set; }
        public double[] FFTYI { get; private set; }

        public ContinuousFFT()
        {
            poslog = new Vector3[64];
            FFTXR = new double[poslog.Length];
            FFTXI = new double[poslog.Length];
            FFTYR = new double[poslog.Length];
            FFTYI = new double[poslog.Length];
        }

        public void Start()
        {
            fft = new FFT2();
            fft.init(6);
        }

        public void Update()
        {
            for (int i = 0; i < poslog.Length - 1; ++i) {
                poslog[i] = poslog[i + 1];
            }
            poslog[poslog.Length - 1] = Managers.personManager.ourPerson.Head.transform.worldToLocalMatrix * gameObject.transform.position;

            for (int i=0; i<poslog.Length; ++i) {
                FFTXI[i] = FFTYI[i] = 0;
            }

            poslog.Select(v => (double)v.x).ToArray().CopyTo(FFTXR, 0);
            fft.run(FFTXR, FFTXI);
            poslog.Select(v => (double)v.y).ToArray().CopyTo(FFTYR, 0);
            fft.run(FFTYR, FFTYI);
        }
    }
}

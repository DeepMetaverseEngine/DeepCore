using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Code.CrashAnalysis
{
    public class CrashAnalyze : MonoBehaviour
    {
        [SerializeField] private bool mReport;
        [SerializeField] private string mAddress;
        
        
        private void Awake()
        {
            Application.logMessageReceived += LogHandle;
            OnInit();
        }

        protected virtual void OnInit() { }

        private void OnDisable()
        {
            OnDisposed();
            Application.logMessageReceived -= LogHandle;
        }

        protected virtual void OnDisposed() { }

        protected virtual void LogHandle(string condition, string stacktrace, LogType type)
        {
            if (type is not LogType.Error or LogType.Exception)
                return;
            
            if (!mReport || string.IsNullOrEmpty(mAddress))
                return;
            StartCoroutine(Report(condition, stacktrace, type));
        }
        
        
        protected virtual IEnumerator Report(string condition, string stacktrace, LogType type)
        {
            var form = new WWWForm();
            form.AddField("message", condition);
            form.AddField("type", type.ToString());
            form.AddField("stacktrace", stacktrace);
            
            var request = UnityWebRequest.Post(mAddress, form);
            yield return request.SendWebRequest();
            
        }
        
        
    }
}
using UnityEngine;
using UnityEngine.UI;


namespace DeepCore.Unity.ResourceViewer
{
    public class Text3D : MonoBehaviour
    {
        [SerializeField]
        private Text _text1;

        [SerializeField]
        private Text _text2;

        public Component ResourceInfo { get; set; }
        public string ResourceName { get; set; }
        public float TextScale
        {
            set
            {
                _text1.transform.localScale = Vector3.one * value;
                _text2.transform.localScale = Vector3.one * value;
            }
        }

        // Update is called once per frame
        void Update()
        {
            transform.rotation = Camera.main.transform.rotation;
            if (ResourceName != _text1.text)
            {
                _text1.text = ResourceName;
            }
            if (ResourceInfo != null && ResourceInfo.ToString() != _text2.text)
            {
                _text2.text = ResourceInfo.ToString();
            }
        }
    }

}
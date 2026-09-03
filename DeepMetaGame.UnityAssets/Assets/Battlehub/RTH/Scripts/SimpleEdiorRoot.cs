using DeepMetaGame.Unity;
using DeepMetaGame.Unity.Preview;
using UnityEngine;

public class SimpleEdiorRoot : MonoBehaviour
{



    [SerializeField] public Transform SceneEditor;
    [SerializeField] public Transform Preview;
    [SerializeField] public Transform Resource;
    [SerializeField] public Transform Battle;

    void Awake()
    {
        BattleBootstrap.OnFinish(Bootstrap); 
    }
    public virtual void Bootstrap()
    {
        var prop = UnityBattleFactory.CommandLineArgs;

        if (prop.TryGetAsBool("-Preview", out var preview) && preview)
        {
            Preview?.gameObject?.SetActive(true);
        }
        else if (prop.TryGetAsBool("-Resource", out var resource) && resource)
        {
            Resource?.gameObject?.SetActive(true);
        }
        else if (prop.TryGetAsBool("-SceneEditor", out var sceneEditor) && sceneEditor)
        {
            SceneEditor?.gameObject?.SetActive(true);
        }
        else if (prop.TryGetAsBool("-Battle", out var battle) && battle)
        {
            Battle?.gameObject?.SetActive(true);
        }
        else if (SceneEditor && SceneEditor.gameObject.activeSelf)
        {
        }
        else if (Preview && Preview.gameObject.activeSelf)
        {
        }
        else if (Resource && Resource.gameObject.activeSelf)
        {
        }
        else if (Battle && Battle.gameObject.activeSelf)
        {
        }
        else
        {
            Preview?.gameObject?.SetActive(true);
        }
    }
}

using UnityEngine;
using System.Collections;

namespace CmdEditor
{
    public class CmdRecv : MonoBehaviour {

	    // Use this for initialization
	    void Start () {
            Debug.Log( CommandLineReader.GetCommandLine() );
            Debug.Log(CommandLineReader.GetCommandLine());
            Debug.Log(CommandLineReader.GetCustomArgument("Language"));
            Debug.Log(CommandLineReader.GetCustomArgument("Version"));
	    }
	
	    // Update is called once per frame
	    void Update () {
	
	    }
    }

}

using UnityEngine;

namespace Code.HUD;

public delegate void OnJoystickHandle(float x, float y, float px, float py);


public interface IJoystickHandle
{
    public enum JoystickState
    {
        Reset,
        Begin,
        Moving,
        End,
    }
    
    GameObject Background { get; set; }
    GameObject Joystick { get; set; }
    GameObject Center { get; set; }
    float Radius { get; set; }
    Rect MoveRect { get; set; }
    
    event OnJoystickHandle OnJoystickRock;
    event OnJoystickHandle OnJoystickStop;


    void Reset(bool stop = false, float x = 0f, float y = 0f, float px = 0f, float py = 0f);
    
    bool TryMoveTo(Vector2 hold, JoystickState state, out Vector2 pos);
    
    
}
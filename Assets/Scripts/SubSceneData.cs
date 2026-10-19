using UnityEngine;

[CreateAssetMenu(fileName = "NewSubScene", menuName = "Game/Sub Scene Data")]
public class SubSceneData : ScriptableObject
{
    [Tooltip("Enter the name of the sub-scene you want to associate with this Scriptable Object.")]
    public string sceneName;

    [Tooltip("Which Row and Column is this sub-scene in?")]
    public Vector2 gridLocation;

    [Tooltip("The position of the center of your sub-scene.")]
    public Vector3 worldLocation;

    [Tooltip("What is the diameter or width of your sub-scene?")]
    public float sceneSize = 50f;

    [Header("Challenge Properties")]
    [Tooltip("Color applied to the player when entering this sub-scene.")]
    public Color playerColor = Color.white;

    [Tooltip("Shape of the player character for this sub-scene.")]
    public PlayerShape playerShape = PlayerShape.Cube;
}

public enum PlayerShape
{
    Cube,
    Sphere,
    Cylinder
}

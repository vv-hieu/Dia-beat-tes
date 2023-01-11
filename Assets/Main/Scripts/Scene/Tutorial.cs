using System.IO;

using UnityEngine;
using UnityEngine.Tilemaps;

public class Tutorial : MonoBehaviour
{
    [SerializeField] private GameObject tutorialZombie;
    [SerializeField] private GameObject weapon;
    [SerializeField] private Tilemap    walkableTilemap;
    [SerializeField] private Tilemap    wallTilemap;
    [SerializeField] private Tile       wallCornerLeft;
    [SerializeField] private Tile       wallCornerRight;
    [SerializeField] private Tile       wallFadeLeft;
    [SerializeField] private Tile       wallFadeRight;
    [SerializeField] private Tile       floorFade;

    private static string PERSISTENT_DATA_PATH = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "tutorial.dat";

    public static bool TutorialDone()
    {
        return File.Exists(PERSISTENT_DATA_PATH);
    }

    public void SpawnTutorialZombie(Transform position)
    {
        Instantiate(tutorialZombie, position.position, Quaternion.identity, transform);
    }

    public void GivePlayerWeapon(LivingEntity player)
    {
        player.SetWeapon(weapon);
    }

    public void SpawnGate()
    {
        wallTilemap.SetTile(new Vector3Int( 8, 6, 0), wallCornerLeft);
        wallTilemap.SetTile(new Vector3Int( 8, 7, 0), wallFadeLeft);
        wallTilemap.SetTile(new Vector3Int( 9, 6, 0), null);
        wallTilemap.SetTile(new Vector3Int(10, 6, 0), null);
        wallTilemap.SetTile(new Vector3Int(11, 6, 0), wallCornerRight);
        wallTilemap.SetTile(new Vector3Int(11, 7, 0), wallFadeRight);

        walkableTilemap.SetTile(new Vector3Int(9, 7, 0), floorFade);
        walkableTilemap.SetTile(new Vector3Int(10, 7, 0), floorFade);
    }

    public void MarkTutorialAsDone()
    {
        using StreamWriter writer = new StreamWriter(PERSISTENT_DATA_PATH);
        writer.Write("mmm yes yes tutorial done mmm uwu");
    }
}

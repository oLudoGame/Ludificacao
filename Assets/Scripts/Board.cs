using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    private readonly Dictionary<GameColor, GameObject> paths = new();
    private readonly Dictionary<GameColor, GameObject> homes = new();
    /// <summary>
    /// Index inicial das casas no caminho branco de cada cor.
    /// </summary>
    private readonly Dictionary<GameColor, int> initialIndexes = new(){
        {GameColor.Blue, 0},
        {GameColor.Red, 13},
        {GameColor.Green, 26},
        {GameColor.Yellow, 39},
    };
    /// <summary>
    ///  Deslocamentos das casas de origem das peças de uma cor.
    /// </summary>
    private readonly Vector3[] offsets = { Vector3.zero, new(1, 0), new(1, 1), new(0, 1) };

    private void Awake()
    {
        foreach (GameColor color in Enum.GetValues(typeof(GameColor)))
        {
            GameObject path = GameObject.Find(color.ToString() + "Path");
            paths.Add(color, path);
            if (color != GameColor.White)
            {
                GameObject home = GameObject.Find(color.ToString() + "Home");
                homes.Add(color, home);
            }
            InitializeTiles(path, color);
        }
    }

    private void InitializeTiles(GameObject path, GameColor color)
    {
        int i = 0;
        int[] safeIndexes = new int[] { 0, 8, 13, 21, 26, 34, 39, 47 };
        foreach (Transform childTransform in path.transform)
        {
            var tile = childTransform.gameObject.AddComponent<Tile>();
            tile.color = color;
            tile.isSafe = false;
            tile.name = color.ToString() + "Tile" + i.ToString();
            if (color == GameColor.White && safeIndexes.Contains(i))
            {
                tile.isSafe = true;
            }
            i++;
        }
    }

    public int GetInitialIndex(GameColor color)
    {
        if (color == GameColor.White) throw new Exception("Branco não é válido");
        return initialIndexes[color];
    }

    public GameObject GetHome(GameColor color)
    {
        if (color == GameColor.White) throw new Exception("Branco não é válido");
        return homes[color];
    }
    public List<Tile> GetTiles(GameColor color)
    {
        return paths[color].GetComponentsInChildren<Tile>().ToList();
    }
    public Vector2 GetHomeOffset(int pieceNumber)
    {
        if (pieceNumber < 0 || pieceNumber > 3) throw new ArgumentOutOfRangeException("O número da peça só pode estar entre 0 e 3");
        return offsets[pieceNumber];
    }

    public static NavigationList<Tile> GetPath(List<Tile> whiteTiles, List<Tile> colorTiles, int startIndex)
    {
        NavigationList<Tile> path = new();
        List<Tile> tilesList = whiteTiles.Skip(startIndex).Concat(whiteTiles.Take(startIndex)).Concat(colorTiles).ToList();
        tilesList.ForEach((t) => path.Add(t));
        path.RemoveAt(tilesList.Count - colorTiles.Count - 1);
        path.Last().isFinal = true;
        return path;
    }

}
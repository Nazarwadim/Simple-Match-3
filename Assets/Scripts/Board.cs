using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine.AI;

public sealed class Board : MonoBehaviour
{
    private const float TweenDuration = 0.2f;

    [SerializeField] private AudioClip _collectSound;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private ScoreCounter _score;
    [SerializeField] private Row[] _rows;

    public static Board Instance { get; private set; }

    public Tile[,] Tiles { get; private set; }

    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);

    private readonly List<Tile> _selection = new();

    private void Awake() => Instance = this;

    private void Start()
    {
        Tiles = new Tile[_rows.Max(row => row.tiles.Length), _rows.Length];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var tile = _rows[y].tiles[x];
                tile.x = x;
                tile.y = y;

                tile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];


                Tiles[x, y] = tile;
            }

        }
    }


    private bool _isSwapping = false;
    public async void Select(Tile tile)
    {
        if (_isSwapping) return;

        if (!_selection.Contains(tile))
        {
            _selection.Add(tile);
        }

        if (_selection.Count < 2)
        {
            return;
        }
        _isSwapping = true;
        await Swap(_selection[0], _selection[1]);

        if (CanPop())
        {
            await Pop();
        }
        else
        {
            await Swap(_selection[0], _selection[1]);
        }

        _selection.Clear();
        _isSwapping = false;
    }


    public async Task Swap(Tile tile1, Tile tile2)
    {
        Image icon1 = tile1.icon;
        Image icon2 = tile2.icon;

        int xAbs = Math.Abs(tile1.x - tile2.x);
        int yAbs = Math.Abs(tile1.y - tile2.y);
        if (Math.Abs(tile1.x - tile2.x) > 1 || Math.Abs(tile1.y - tile2.y) > 1 || yAbs + xAbs == 2)
        {
            Color color1 = icon1.color;
            Color color2 = icon2.color;
            icon1.color = new Color(1, color1.g, color1.b);
            Sequence seq1 = DOTween.Sequence();
            seq1.Append(icon1.DOColor(new Color(1, 0, 0), TweenDuration / 4))
                .Join(icon2.DOColor(new Color(1, 0, 0), TweenDuration / 4))
                .Append(icon1.DOColor(color1, TweenDuration / 4))
                .Join(icon2.DOColor(color2, TweenDuration / 4));
            await seq1.Play().AsyncWaitForCompletion();
            return;
        }

        Sequence sequence = DOTween.Sequence();

        sequence.Join(icon1.transform.DOMove(icon2.transform.position, TweenDuration))
            .Join(icon2.transform.DOMove(icon1.transform.position, TweenDuration));

        await sequence.Play().AsyncWaitForCompletion();

        icon1.transform.SetParent(tile2.transform);
        icon2.transform.SetParent(tile1.transform);

        tile1.icon = icon2;
        tile2.icon = icon1;

        (tile2.Item, tile1.Item) = (tile1.Item, tile2.Item);
    }

    private bool CanPop()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (Tiles[x, y].GetConnectedTiles().Count >= 3)
                {
                    return true;
                }
            }
        }

        return false;
    }


    private async Task Pop()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tile tile = Tiles[x, y];

                var connectedTiles = tile.GetConnectedTiles();

                if (connectedTiles.Count < 3) continue;

                var deflateSequence = DOTween.Sequence();

                foreach (Tile connectedTile in connectedTiles)
                {
                    deflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.zero, TweenDuration));
                }
                
                _audioSource.PlayOneShot(_collectSound);

                await deflateSequence.Play().AsyncWaitForCompletion();

                var inflateSequence = DOTween.Sequence();

                _score.Score += tile.Item.value * connectedTiles.Count;

                foreach (Tile connectedTile in connectedTiles)
                {
                    connectedTile.Item = ItemDatabase.Items[UnityEngine.Random.Range(0, ItemDatabase.Items.Length)];
                    inflateSequence.Join(connectedTile.icon.transform.DOScale(Vector3.one, TweenDuration));
                }

                await inflateSequence.Play().AsyncWaitForCompletion();

                x = 0;
                y = 0;
            }
        }
    }
}

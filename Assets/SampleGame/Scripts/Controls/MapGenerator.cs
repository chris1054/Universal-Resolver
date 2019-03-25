﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using App.Scripts.Boards;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityIoC;


[ProcessingOrder(1)]
public class MapGenerator : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private Button btnRestart;
    [SerializeField] private RectTransform container;

    [Prefab] private Cell cell;
    [Singleton] private List<Cell> cells = new List<Cell>();
    [Singleton] private List<CellData> cellData = new List<CellData>();
    [Singleton] private IGameSolver gameSolver;
    [Singleton] private IGameBoard gameBoard;

    private GameSetting gameSetting = new GameSetting();
    private ReactiveProperty<GameStatus> gameStatus = new ReactiveProperty<GameStatus>();

    public void Start()
    {
        if (!AssemblyContext.IsDefaultInstanceInitialized)
        {
            AssemblyContext.GetDefaultInstance(this);
        }
        
        //setup game status, when it get changes
        gameStatus.Subscribe(status => { print("Game status: " + status.ToString()); })
            .AddTo(gameObject);

        //setup button restart
        if (btnRestart)
        {
            btnRestart.gameObject.SetActive(false);
            btnRestart.OnClickAsObservable()
                .Subscribe(unit => { StartCoroutine(RestartRoutine()); })
                .AddTo(gameObject);
        }

        //setup the layout
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gameSetting.Width;

        //build the board
        gameBoard.Build();

        //create cells
        foreach (var data in cellData)
        {
            var cell = Object.Instantiate(this.cell);
            cell.SetParent(container);
            cell.SetCellData(data);
            cells.Add(cell);
        }

        //solve the game
        Observable.FromCoroutine(_ => gameSolver.Solve(1f)).Subscribe(_ =>
            {
                print("Finished");
                btnRestart.gameObject.SetActive(true);
            })
            .AddTo(this);
    }

    IEnumerator RestartRoutine()
    {
        AssemblyContext.DisposeDefaultInstance();
        yield return null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); //restart the game
    }
}
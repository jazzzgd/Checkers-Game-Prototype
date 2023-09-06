﻿using System;
using System.Linq;
using UnityEngine;

namespace Checkers
{
    public class GameController : MonoBehaviour, IGameController, IGameObservable
    {
        [Serializable]
        public class FieldRow
        {
            public ChipComponent[] Chips;
        }

        [SerializeField]
        private FieldRow[] _chipsOnField;
        [SerializeField]
        private CellComponent[] _cells;
        [SerializeField]
        private Material _focusedMaterial;
        [SerializeField]
        private GameObject _camera;

        private CellComponent _focusedCell;
        private ChipComponent _selectedChip;
        private Tuple<int, int> _selectedChipPosition;
        private ColorType _stepColorSide = ColorType.White;

        public event OnStepEventHandler OnStep;
        public event OnEatChipEventHandler OnEatChip;
        public event OnSelectChipEventHandler OnSelectChip;

        private void Start()
        {
            foreach (var cell in _cells)
            {
                cell.OnFocusEventHandler += OnFocusCell;
                cell.OnClickEventHandler += OnClickCell;
            }
        }

        private void Update()
        {
            UpdateFocusedCell();
            UpdateChipSelection();
        }

        private void OnFocusCell(CellComponent cell, bool focused)
        {
            if (focused)
                cell.AddAdditionalMaterial(_focusedMaterial);
            else
                cell.RemoveAdditionalMaterial();
        }

        private void OnClickCell(BaseClickComponent cell)
        {
            var x = (int)cell.transform.position.x;
            var z = -(int)cell.transform.position.z;
            Debug.Log($"Выбрана клетка: {x} , {z}");

            var chip = _chipsOnField[z]?.Chips[x];
            if (chip != null && chip.GetColor == _stepColorSide)
            {
                SelectChip(chip);
                _selectedChipPosition = new Tuple<int, int>(x, z);
                var onSelectChipArgs = new CellCoordsEventArgs(x, z);
                OnSelectChip?.Invoke(this, onSelectChipArgs);
            }
            else if (_selectedChip != null)
            {
                var cellIsEmpty = _chipsOnField[z].Chips[x] == null;
                var isDiagonalStep = Math.Abs(x - _selectedChipPosition.Item1) == Math.Abs(z - _selectedChipPosition.Item2);
                var canMakeStep = cellIsEmpty && isDiagonalStep && _focusedCell.GetColor == ColorType.Black && _selectedChip.GetColor == _stepColorSide;
                if (canMakeStep)
                {

                    MakeStep(_selectedChipPosition.Item1, _selectedChipPosition.Item2, x, z);
                    string winMessage;
                    if (CheckWin(out winMessage))
                        Debug.Log(winMessage);
                }
                else
                {
                    var message = "Не верный шаг";
                    if (!cellIsEmpty)
                        message = "Клетка занята";
                    else if (!isDiagonalStep)
                        message = "Ход должен быть по диагонали";
                    else if (_focusedCell.GetColor != ColorType.Black)
                        message = "Клетка должна быть черной";
                    else if (_selectedChip.GetColor == _stepColorSide)
                        message = "Сейчас ход " + _stepColorSide.ToString();
                    Debug.Log(message);
                }
            }
        }

        private void UpdateFocusedCell()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(ray, out hit);
            var hittingCell = hit.transform?.GetComponent<CellComponent>();
            if (hittingCell != null && _focusedCell != hittingCell)
            {
                if (_focusedCell != null)
                {
                    _focusedCell.OnPointerExit();
                    _focusedCell = null;
                }
                hittingCell.OnPointerEnter();
                _focusedCell = hittingCell;
            }
        }

        private void UpdateChipSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _focusedCell.OnPointerClick();
            }
        }

        private void SelectChip(ChipComponent chip)
        {
            if (_selectedChip != null)
                _selectedChip.RemoveAdditionalMaterial();

            _selectedChip = chip;
            chip.AddAdditionalMaterial(_focusedMaterial);
        }

        public void MakeStep(int xFrom, int zFrom, int xTo, int zTo)
        {
            if (_selectedChip == null)
            {
                _selectedChip = _chipsOnField[zFrom].Chips[xFrom];
            }
            var eatenEnemy = EatenEnemyForStep(xFrom, zFrom, xTo, zTo);
            if (eatenEnemy != null)
            {
                var eatenChip = _chipsOnField[eatenEnemy.Item2].Chips[eatenEnemy.Item1];
                Destroy(eatenChip.gameObject);

                var onEatChipArgs = new CellCoordsEventArgs(eatenEnemy.Item1, eatenEnemy.Item2);
                OnEatChip?.Invoke(this, onEatChipArgs);

                _chipsOnField[eatenEnemy.Item2].Chips[eatenEnemy.Item1] = null;
            }

            _chipsOnField[zTo].Chips[xTo] = _selectedChip;
            _chipsOnField[zFrom].Chips[xFrom] = null;
            _selectedChip.GetComponent<ChipMove>().MoveToPosition(xTo, -zTo);
            _camera.GetComponent<CameraMove>().MoveToAnotherSide();
            _selectedChip.RemoveAdditionalMaterial();
            _selectedChip = null;
            _selectedChipPosition = null;

            var onStepArgs = new OnStepEventArgs(_stepColorSide,new Tuple<int, int>(xFrom, zFrom),new Tuple<int, int>(xTo, zTo));
            OnStep?.Invoke(this, onStepArgs);

            switch (_stepColorSide)
            {
                case ColorType.White:
                    _stepColorSide = ColorType.Black;
                    break;
                case ColorType.Black:
                    _stepColorSide = ColorType.White;
                    break;
            }
        }

        private Tuple<int, int> EatenEnemyForStep(int xFrom, int zFrom, int xTo, int zTo)
        {
            var cellsCrossedCount = Math.Abs(xTo - xFrom);
            var longStep = (cellsCrossedCount) == 2;
            if (longStep)
            {
                for (var i = 1; i < cellsCrossedCount; i++)
                {
                    var xToCheck = xTo - xFrom > 0 ? xFrom + i : xFrom - i;
                    var zToCheck = zTo - zFrom > 0 ? zFrom + i : zFrom - i;
                    if (_chipsOnField[zToCheck].Chips[xToCheck] != null)
                    {
                        return new Tuple<int, int>(xToCheck, zToCheck);
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        private bool CheckWin(out string winMessage)
        {
            var allChips = _chipsOnField
                .SelectMany(row => row.Chips)
                .Where(chip => chip != null);
            
            var whiteWin = allChips.Where(chip => chip.GetColor == ColorType.Black).Count() <= 0
                || _chipsOnField[7].Chips.Where(chip => chip != null && chip.GetColor == ColorType.White).Count() > 0;
            
            var blackWin = allChips.Where(chip => chip.GetColor == ColorType.White).Count() <= 0
                || _chipsOnField[0].Chips.Where(chip => chip != null && chip.GetColor == ColorType.Black).Count() > 0;
            winMessage = whiteWin ? "Победили белые!" : (blackWin ? "Победили черные!" : "");
            
            return whiteWin || blackWin;
        }
    }
}
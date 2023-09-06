using System.Collections;
using System.IO;
using UnityEngine;
using Zenject;

namespace Checkers
{
    public class GameObserver : MonoBehaviour
    {
        public enum ObserverMode
        {
            Observe,
            Play
        }

        [SerializeField]
        private ObserverMode _mode = ObserverMode.Observe;
        [Inject]
        private IGameObservable _gameObservable;
        [Inject]
        private IGameController _gameController;

        private string _gameLogPath{get => Application.persistentDataPath + "/Game";}

        private void Awake()
        {
            _gameObservable.OnEatChip += OnEatChip;
            _gameObservable.OnSelectChip += OnSelectChip;
            _gameObservable.OnStep += OnStep;

            if (_mode == ObserverMode.Observe)
            {
                if (File.Exists(_gameLogPath))
                    File.Delete(_gameLogPath);
            }
            else
            {
                if (File.Exists(_gameLogPath))
                {
                    StopAllCoroutines();
                    StartCoroutine(PlayGameFromFile());
                }
                else
                {
                    Debug.Log("Файл не найден");
                }
            }
        }

        private IEnumerator PlayGameFromFile()
        {
            yield return new WaitForSeconds(2f);
            foreach (string line in File.ReadAllLines(_gameLogPath))
            {
                var step = new OnStepEventArgs(line);
                _gameController.MakeStep(
                    step.From.Item1,
                    step.From.Item2,
                    step.To.Item1,
                    step.To.Item2
                );
                yield return new WaitForSeconds(3f);
            }
        }

        private void OnStep(object sender, OnStepEventArgs args)
        {
            Debug.Log("Ход " + args.Side.ToString() + "\n из : " + args.From.Item1 + ", " + args.From.Item2 + " в : " + args.To.Item1 + ", " + args.To.Item2);
            var stepString = args.ToSerializedString();
            AddToFile(stepString);
        }

        private void OnSelectChip(object sender, CellCoordsEventArgs args)
        {
            Debug.Log("Выбрана шашка: " + args.x + ", " + args.z);
            string json = JsonUtility.ToJson(args);
        }

        private void OnEatChip(object sender, CellCoordsEventArgs args)
        {
            Debug.Log("Съесть шашку: " + args.x + ", " + args.z);
            string json = JsonUtility.ToJson(args);
        }

        private void AddToFile(string content)
        {
            if (File.Exists(_gameLogPath))
                using (StreamWriter sw = File.AppendText(_gameLogPath))
                {
                    sw.WriteLine(content);
                }
            else
                File.WriteAllText(_gameLogPath, content + "\n");

            Debug.Log("Записано");
        }
    }
}
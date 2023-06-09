﻿using CarWash.cars;

namespace CarWash
{
    public delegate void PostFreeHandler(Post post);
    public delegate void CarWashingInfoHandler(Post post, ICar currentCar);

    /// <summary>
    /// Перечисление статусов поста (свободен, занят мойкой).
    /// </summary>
    public enum PostStatus
    {
        Free,
        Washing
    }

    /// <summary>
    /// Класс поста.
    /// </summary>
    public class Post
    {
        private event PostFreeHandler? OnPostFree;              // Вызывается, когда пост свободен.
        private event CarWashingInfoHandler? OnCarWashingInfo;  // Вызывается перед началом мойки и по её окончанию.
                                                                // Нужен для получения информации о процессе мойки.
        /// <summary>
        /// Идентификатор поста (чтобы хоть как-то их различать).
        /// </summary>
        public int Id { get; private set; }
        /// <summary>
        /// Статус поста.
        /// </summary>
        public PostStatus Status { get; private set; }
        /// <summary>
        /// Конструктор поста.
        /// </summary>
        /// <param name="id">Идентификатор поста.</param>
        /// <param name="postFreeHandler">Делегат для обработчика события освобождения поста.</param>
        public Post(int id, PostFreeHandler postFreeHandler)
        {
            Status = PostStatus.Free;
            Id = id;
            OnPostFree += postFreeHandler;
        }
        /// <summary>
        /// Конструктор поста.
        /// </summary>
        /// <param name="id">Идентификатор поста.</param>
        /// <param name="postFreeHandler">Делегат для обработчика события освобождения поста.</param>
        /// <param name="carWashingInfoHandler">Делегат для обработчика события начала и окончания мойки.</param>
        public Post(int id, PostFreeHandler postFreeHandler, CarWashingInfoHandler carWashingInfoHandler) : this(id, postFreeHandler)
        {
            OnCarWashingInfo += carWashingInfoHandler;
        }
        /// <summary>
        /// Метод, имитирующий процесс мойки.
        /// </summary>
        /// <param name="car">Машина, которую нужно помыть.</param>
        public void StartWashing(ICar car)
        {
            Task.Factory.StartNew(async () =>                   // "Моем" авто в отдельном потоке.
            {
                Status = PostStatus.Washing;

                OnCarWashingInfo?.Invoke(this, car);            // Вызов обработчика события перед началом мойки.

                await Task.Delay((int)car.WashingTime * 1000);  // Имитация мойки.

                Status = PostStatus.Free;

                OnCarWashingInfo?.Invoke(this, car);            // Вызов обработчика после мойки.

                OnPostFree?.Invoke(this);                       // Оповещаем класс Wash о том, что мойка авто закончена.
                                                                // Приступаем к мойке следующего авто.
            });
        }
    }

    /// <summary>
    /// Класс, выполняющий функцию автомойки.
    /// </summary>
    public class Wash
    {
        /// <summary>
        /// Максимальное количество постов.
        /// </summary>
        private const int _maxPosts = 6;            // Выяснил, что их обычно от 4 до 6, поэтому взял максимальное для этой задачи.
        /// <summary>
        /// Очередь из авто.
        /// </summary>
        private Queue<ICar> _queueCars;
        /// <summary>
        /// Список постов.
        /// </summary>
        private List<Post> _posts;
        /// <summary>
        /// Конструктор класса автомойки.
        /// </summary>
        public Wash()
        {
            _queueCars = new Queue<ICar>();
            _posts = new List<Post>();

            for (int i = 1; i <= _maxPosts; i++) // Заполнение списка постов (всего 6, каждому присваиваем свой идентификатор, от 1 до 6)
                _posts.Add(new Post(i, PostFree));
        }
        /// <summary>
        /// Конструктор класса автомойки.
        /// </summary>
        /// <param name="handler">Делегат для обработчика события начала и окончания мойки.</param>
        public Wash(CarWashingInfoHandler handler)
        {
            _queueCars = new Queue<ICar>();
            _posts = new List<Post>();

            // Типа рекурсивный вызов через обработчик события. Решил сделать так, чтобы класс Post не хранил в себе очередь машин.
            // (да и вообще что-то "знал" об этой очереди)

            for (int i = 1; i <= _maxPosts; i++)
                _posts.Add(new Post(i, PostFree, handler));
        }
        /// <summary>
        /// Обработчик для события OnPostFree.
        /// </summary>
        /// <param name="post"></param>
        private void PostFree(Post post)
        {
            if (_queueCars.TryDequeue(out ICar? nextCar))
                post.StartWashing(nextCar);
        }
        /// <summary>
        /// Метод, запускающий работу мойки.
        /// </summary>
        /// <exception cref="NoCarsException">Если вдруг автомобилей вообще нет.</exception>
        public void StartWorking()
        {
            if (_queueCars.Count == 0) throw new NoCarsException();

            foreach (Post post in _posts)
                if (_queueCars.TryDequeue(out ICar? car)) post.StartWashing(car);
        }
        /// <summary>
        /// Метод для добавления новых авто в очередь.
        /// </summary>
        /// <param name="car">Новый автомобиль.</param>
        public void AddCar(ICar car) => _queueCars?.Enqueue(car);
    }

    public class NoCarsException : Exception
    {
        public NoCarsException() : base() { }
        public NoCarsException(string message) : base(message) { }
    }
}

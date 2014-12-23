using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ProducerWorkerThread
{
    public class SyncEvents
    {
        public EventWaitHandle ExitThreadEvent
        {
            get { return _exitThreadEvent; }
        }
        public EventWaitHandle NewItemEvent
        {
            get { return _newItemEvent; }
        }
        public WaitHandle[] EventArray
        {
            get { return _eventArray; }
        }

        private EventWaitHandle _newItemEvent;
        private EventWaitHandle _exitThreadEvent;
        private WaitHandle[] _eventArray;

        public SyncEvents()
        {
            _newItemEvent = new AutoResetEvent(false);
            _exitThreadEvent = new ManualResetEvent(false);
            _eventArray = new WaitHandle[2];
            _eventArray[0] = _newItemEvent;
            _eventArray[1] = _exitThreadEvent;
        }
    }

    public class Producer
    {
        private Queue<int> _queue;
        private SyncEvents _syncEvents;
        
        public Producer(Queue<int> q, SyncEvents e)
        {
            _queue = q;
            _syncEvents = e;
        }

        // Producer.ThreadRun
        public void ThreadRun()
        {
            int count = 0;
            Random r = new Random();
            while (!_syncEvents.ExitThreadEvent.WaitOne(0, false))
            {
                lock (((ICollection)_queue).SyncRoot)
                {
                    while (_queue.Count < 20)
                    {
                        _queue.Enqueue(r.Next(0, 100));
                        _syncEvents.NewItemEvent.Set();
                        count++;
                    }
                }
            }
            Console.WriteLine("Producer thread: produced {0} items", count);
        }
    }

    public class Consumer
    {
        public Consumer(Queue<int> q, SyncEvents e)
        {
            _queue = q;
            _syncEvents = e;
        }

        // Consumer.ThreadRun
        public void ThreadRun()
        {
            int count = 0;
            while (WaitHandle.WaitAny(_syncEvents.EventArray) != 1)
            {
                lock (((ICollection)_queue).SyncRoot)
                {
                    int item = _queue.Dequeue();
                }
                count++;
            }
            Console.WriteLine("Consumer Thread: consumed {0} items", count);
        }
        private Queue<int> _queue;
        private SyncEvents _syncEvents;
    }
    
    public class ThreadSyncSample
    {
        private static void ShowQueueContents(Queue<int> q)
        {
            lock (((ICollection)q).SyncRoot)
            {
                foreach (int item in q)
                {
                    Console.Write("{0} ", item);
                }
            }
            Console.WriteLine();
        }
        
        static void Main(string[] args)
        {
            Queue<int> queue = new Queue<int>();
            SyncEvents syncevents = new SyncEvents();

            Console.WriteLine("Configuring worker threads...");

            Producer producer = new Producer(queue, syncevents);
            Consumer consumer = new Consumer(queue, syncevents);

            Thread producerThread = new Thread(producer.ThreadRun);
            Thread consumerThread = new Thread(consumer.ThreadRun);

            Console.WriteLine("Launching producer and consumer threads...");
            producerThread.Start();
            consumerThread.Start();

            for (int i = 0; i < 4; i++)
            {
                Thread.Sleep(2500);
                ShowQueueContents(queue);
            }

            Console.WriteLine("Signaling threads to terminate...");
            syncevents.ExitThreadEvent.Set();

            producerThread.Join();
            consumerThread.Join();

            Console.Read();
        }
    }
}

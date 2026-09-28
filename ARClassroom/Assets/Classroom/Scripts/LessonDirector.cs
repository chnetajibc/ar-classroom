using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARClassroom
{
    [System.Serializable]
    public class Lesson
    {
        public string title;
        public Color accent = new Color(0.4f, 0.9f, 1f);
        public string[] bullets;
        public string[] code;
        public string[] explanation;
        public string question;
        public string answer;
    }

    /// <summary>Runs the classroom "scene": the tutor teaches at the board, explains to the class, asks a question and a student answers.</summary>
    public class LessonDirector : MonoBehaviour
    {
        public TutorBehaviour tutor;
        public StudentBehaviour[] students;
        public BoardText board;
        public Transform boardWritePoint;
        public Transform boardStandPoint;
        public Transform stageCenter;
        public Transform classFocus;
        public ClassroomUI ui;
        public List<Lesson> lessons = new List<Lesson>();

        int lessonIndex;
        Coroutine running;
        static readonly string[] Praise = { "Exactly right, well done!", "Correct! Nice thinking.", "Yes, that's it. Good answer!" };

        void Start()
        {
            if (lessons.Count == 0) lessons = DefaultLessons();
            running = StartCoroutine(Loop());
        }

        public string CurrentTitle => lessons.Count > 0 ? lessons[lessonIndex % lessons.Count].title : "";

        public void NextLesson()
        {
            ResetActors();
            lessonIndex++;
            running = StartCoroutine(Loop());
        }

        public void CallOn(StudentBehaviour s)
        {
            ResetActors();
            running = StartCoroutine(SpontaneousAnswer(s));
        }

        void ResetActors()
        {
            StopAllCoroutines();
            foreach (var st in students) st.ResetState();
            tutor.Rig.ik.rightHandTarget = null;
            tutor.Rig.ik.rightElbowHint = null;
            tutor.Talk(true);
        }

        IEnumerator SpontaneousAnswer(StudentBehaviour s)
        {
            tutor.LookAt(s.Rig.LookPoint);
            ui?.Say(tutor.TutorName, s.StudentName + ", would you like to add something?", 3f);
            yield return tutor.FaceTowards(s.transform.position);
            yield return tutor.Gesture();
            ui?.Say(s.StudentName, "Yes! " + Current().answer, 4f);
            yield return s.Answer(4f);
            yield return tutor.Nod();
            running = StartCoroutine(Loop());
        }

        Lesson Current() { return lessons[lessonIndex % lessons.Count]; }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(0.6f);
            while (true)
            {
                yield return RunLesson(Current());
                lessonIndex++;
                yield return new WaitForSeconds(1.5f);
            }
        }

        IEnumerator RunLesson(Lesson l)
        {
            ui?.SetLesson(l.title);
            // walk to the board and write the notes
            ui?.Say(tutor.TutorName, "Let's get started. Today's topic: " + l.title + ".", 3.5f);
            yield return tutor.WalkTo(boardStandPoint.position);
            yield return tutor.FaceTowards(boardWritePoint.position);
            board.Clear();
            board.SetAccent(l.accent);
            int rows = 1 + l.bullets.Length + l.code.Length;
            var tut = StartCoroutine(tutor.Write(boardWritePoint, 1.2f + l.bullets.Length * 1.6f + l.code.Length * 1.1f + 0.5f));
            boardWritePoint.position = board.RowWorld(0, 0.3f);
            yield return board.WriteTitle(l.title, 1.2f);
            for (int i = 0; i < l.bullets.Length; i++)
            {
                boardWritePoint.position = board.RowWorld(1 + i, 0.3f);
                yield return board.WriteBullet(i, l.bullets[i], 1.6f);
            }
            for (int i = 0; i < l.code.Length; i++)
            {
                boardWritePoint.position = board.RowWorld(1 + BoardText.MaxBullets + i, 0.3f);
                yield return board.WriteCode(i, l.code[i], 1.1f);
            }
            yield return tut;

            // turn to the class and explain
            yield return tutor.WalkTo(stageCenter.position);
            yield return tutor.FaceTowards(classFocus.position);
            tutor.Talk(true);
            foreach (var line in l.explanation)
            {
                tutor.LookAt(RandomStudent().Rig.LookPoint);
                ui?.Say(tutor.TutorName, line, 4.5f);
                yield return tutor.Gesture();
                yield return new WaitForSeconds(2.2f);
                tutor.LookAt(classFocus);
            }

            // question: hands go up, one student is picked and answers
            ui?.Say(tutor.TutorName, l.question, 4f);
            tutor.LookAt(classFocus);
            yield return tutor.Gesture();
            var raised = PickStudents(3);
            foreach (var s in raised) { s.RaiseHand(true); yield return new WaitForSeconds(Random.Range(0.3f, 0.7f)); }
            yield return new WaitForSeconds(2f);
            var chosen = raised[Random.Range(0, raised.Count)];
            tutor.LookAt(chosen.Rig.LookPoint);
            ui?.Say(tutor.TutorName, chosen.StudentName + ", go ahead.", 2.5f);
            yield return tutor.FaceTowards(chosen.transform.position);
            yield return tutor.Gesture();
            foreach (var s in raised) if (s != chosen) s.RaiseHand(false);
            ui?.Say(chosen.StudentName, l.answer, 5f);
            yield return chosen.Answer(5f);
            yield return tutor.FaceTowards(classFocus.position);
            ui?.Say(tutor.TutorName, Praise[Random.Range(0, Praise.Length)], 3f);
            yield return tutor.Nod();
            yield return new WaitForSeconds(2f);
        }

        StudentBehaviour RandomStudent() { return students[Random.Range(0, students.Length)]; }

        List<StudentBehaviour> PickStudents(int n)
        {
            var pool = new List<StudentBehaviour>(students);
            var picked = new List<StudentBehaviour>();
            for (int i = 0; i < n && pool.Count > 0; i++)
            {
                int k = Random.Range(0, pool.Count);
                picked.Add(pool[k]);
                pool.RemoveAt(k);
            }
            return picked;
        }

        static List<Lesson> DefaultLessons()
        {
            return new List<Lesson>
            {
                new Lesson
                {
                    title = "1/7  ARRAYS  -  O(1) access", accent = new Color(0.25f, 0.85f, 1f),
                    bullets = new[] { "- Contiguous memory", "- arr[i] = base + i * size", "- Insert at end O(1), middle O(n)", "- Great for cache + index lookup" },
                    code = new[] { "int[] a = {10,20,30};", "// read:  a[1] -> 20   O(1)", "// push:  O(1) / O(n) if resize" },
                    explanation = new[] { "An array keeps its items side by side in memory.", "So the computer finds arr[i] with one calculation: base plus i times size.", "Inserting in the middle is slower, because every later item has to shift." },
                    question = "Why is reading arr[5] so fast?",
                    answer = "Because the address is base plus index times size, one step, so it is O(1).",
                },
                new Lesson
                {
                    title = "2/7  LINKED LIST  -  O(1) insert", accent = new Color(0.35f, 1f, 0.55f),
                    bullets = new[] { "- Nodes + pointers, not contiguous", "- Insert/delete at head O(1)", "- Search O(n): must walk the list", "- No resize cost like arrays" },
                    code = new[] { "class Node { int val; Node next; }", "// head -> [10] -> [20] -> null", "// insert at head: O(1)" },
                    explanation = new[] { "Each node stores a value and a pointer to the next node.", "Adding at the head only changes one pointer, so it is O(1).", "But to find a value, you must walk the list from the start." },
                    question = "When is a linked list better than an array?",
                    answer = "When we insert or delete at the head a lot, since nothing has to shift.",
                },
                new Lesson
                {
                    title = "3/7  STACK & QUEUE", accent = new Color(1f, 0.8f, 0.3f),
                    bullets = new[] { "- Stack: LIFO - Push / Pop / Peek O(1)", "- Queue: FIFO - Enqueue / Dequeue O(1)", "- Undo, BFS, brackets, sliding window" },
                    code = new[] { "Stack<int> s = new();", "s.Push(1); s.Pop();     // LIFO", "Queue<int> q = new();", "q.Enqueue(1); q.Dequeue(); // FIFO" },
                    explanation = new[] { "A stack is like a pile of plates: last in, first out.", "A queue is like the canteen line: first in, first out.", "Undo uses a stack. Breadth-first search uses a queue." },
                    question = "Which structure would you use for an undo button?",
                    answer = "A stack, because the last action is the first one we undo.",
                },
                new Lesson
                {
                    title = "4/7  BIG-O CHEAT SHEET", accent = new Color(1f, 0.45f, 0.6f),
                    bullets = new[] { "- O(1) < O(log n) < O(n) < O(n log n) < O(n^2)", "- Binary search is O(log n)", "- Two pointers saves a loop", "- Hash map trades space for time" },
                    code = new[] { "// n = 1,000,000", "// O(n^2)     ~ 31 years (bad!)", "// O(n log n) ~ 20M ops (good)" },
                    explanation = new[] { "Big-O tells us how running time grows as the input grows.", "Always think about Big-O before you start coding.", "One million items in O(n squared) is hopeless, but O(n log n) is easy." },
                    question = "Which grows faster, O(n) or O(n squared)?",
                    answer = "O(n squared) grows much faster, so O(n) wins on large inputs.",
                },
                new Lesson
                {
                    title = "5/7  BINARY SEARCH TREE", accent = new Color(0.7f, 0.6f, 1f),
                    bullets = new[] { "- Left < Root < Right", "- Search / Insert average O(log n)", "- Worst case O(n) if unbalanced", "- AVL / Red-Black keep it balanced" },
                    code = new[] { "      8", "    /   \\", "   3     10", "  / \\      \\", " 1   6      14", "// search(6): 8 -> 3 -> 6  O(log n)" },
                    explanation = new[] { "In a binary search tree, smaller values go left and larger go right.", "Each comparison throws away half of the tree.", "If the tree becomes a chain, search degrades to O(n)." },
                    question = "How do we find 6 in this tree?",
                    answer = "Start at 8, go left to 3, then right to 6. Three steps.",
                },
                new Lesson
                {
                    title = "6/7  QUICK REFERENCE", accent = new Color(0.45f, 0.7f, 1f),
                    bullets = new[] { "- BIG-O: O(1) FAST, O(n^2) SLOW", "- ARRAYS: O(1) lookup", "- LISTS: O(n) search", "- MAPS: O(1) average" },
                    code = new[] { "// O(1) FAST, O(n^2) SLOW", "// arrays O(1), lists O(n)", "// maps O(1) avg" },
                    explanation = new[] { "Let's recap with a quick reference sheet.", "Arrays and hash maps give constant-time lookup; lists need a walk." },
                    question = "Which structure gives O(1) average lookup by key?",
                    answer = "A hash map.",
                },
                new Lesson
                {
                    title = "7/7  DSA MOTTO", accent = new Color(0.4f, 0.9f, 0.7f),
                    bullets = new[] { "- CODE / DEBUG / REPEAT", "- STAY CURIOUS", "- LEARN SOMETHING DAILY", "- O(log n) MINDSET" },
                    code = new[] { "// DATA STRUCTURES &", "// ALGORITHMS" },
                    explanation = new[] { "Code, debug, repeat. Stay curious and learn something every day.", "Keep an O(log n) mindset: cut the problem in half." },
                    question = "What is one thing you will practise this week?",
                    answer = "I will practise binary search and trees.",
                },
            };
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyQueue.Tests
{
    /// <summary>Touch/mouse tap detection, simulated with the Input System test fixture (no editor focus needed).</summary>
    public class TapInputTests : InputTestFixture
    {
        TapInput taps;
        Touchscreen touch;

        public override void Setup()
        {
            base.Setup();
            touch = InputSystem.AddDevice<Touchscreen>();
            taps = new TapInput();
        }

        public override void TearDown()
        {
            taps.Dispose();
            base.TearDown();
        }

        [Test]
        public void QuickTap_PressAndReleaseInTheSameUpdate_IsNotMissed()
        {
            var p = new Vector2(100, 200);
            BeginTouch(1, p, queueEventOnly: true);
            EndTouch(1, p, queueEventOnly: true);
            InputSystem.Update();

            Vector2 got;
            Assert.IsTrue(taps.TryDequeue(out got), "a tap that is down and up inside one update must still be seen");
            Assert.AreEqual(p, got);
            Assert.IsFalse(taps.TryDequeue(out got), "exactly one tap");
        }

        [Test]
        public void SecondFinger_WhileTheFirstIsStillDown_IsDetected()
        {
            var a = new Vector2(120, 220);
            var b = new Vector2(400, 700);
            BeginTouch(1, a);            // finger 1 goes down and stays down
            BeginTouch(2, b);            // finger 2 taps meanwhile

            var seen = new System.Collections.Generic.List<Vector2>();
            Vector2 got;
            while (taps.TryDequeue(out got)) seen.Add(got);
            CollectionAssert.AreEqual(new[] { a, b }, seen, "both fingers produce a tap at their own position");
        }

        [Test]
        public void EachTap_ReportsThePositionOfTheFingerThatPressed()
        {
            var first = new Vector2(50, 60);
            var second = new Vector2(500, 900);
            BeginTouch(7, first); EndTouch(7, first);
            BeginTouch(8, second); EndTouch(8, second);

            Vector2 got;
            Assert.IsTrue(taps.TryDequeue(out got)); Assert.AreEqual(first, got);
            Assert.IsTrue(taps.TryDequeue(out got)); Assert.AreEqual(second, got);
        }

        [Test]
        public void MouseClick_IsDetected_ForEditorPlay()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            Set(mouse.position, new Vector2(33, 44));
            Press(mouse.leftButton);
            Release(mouse.leftButton);

            Vector2 got;
            Assert.IsTrue(taps.TryDequeue(out got));
            Assert.AreEqual(new Vector2(33, 44), got);
        }

        [Test]
        public void ReleasingAFinger_IsNotATap()
        {
            var p = new Vector2(10, 10);
            BeginTouch(1, p, queueEventOnly: true);
            InputSystem.Update();
            Vector2 got;
            Assert.IsTrue(taps.TryDequeue(out got));
            EndTouch(1, p);
            Assert.IsFalse(taps.TryDequeue(out got));
        }

        [Test]
        public void TheQueue_IsBounded_AndClearEmptiesIt()
        {
            for (int i = 1; i <= 20; i++) { BeginTouch(i, new Vector2(i, i)); EndTouch(i, new Vector2(i, i)); }
            int count = 0; Vector2 got;
            while (taps.TryDequeue(out got)) count++;
            Assert.LessOrEqual(count, 8, "a flood of taps must not grow the queue without bound");

            BeginTouch(99, Vector2.one); EndTouch(99, Vector2.one);
            taps.Clear();
            Assert.IsFalse(taps.TryDequeue(out got));
        }
    }
}

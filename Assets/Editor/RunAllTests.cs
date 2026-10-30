using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace Obscure.Editor
{
    public static class RunAllTests
    {
        class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var summary = $"[TEST_RESULTS] Passed: {result.PassCount}, Failed: {result.FailCount}, Inconclusive: {result.InconclusiveCount}, Skipped: {result.SkipCount}";
                Debug.Log(summary);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine(summary);
                void CollectFailures(ITestResultAdaptor r)
                {
                    if (r.TestStatus == TestStatus.Failed)
                    {
                        if (!r.HasChildren)
                            sb.AppendLine($"FAILED: {r.Test.FullName}\n{r.Message}\n{r.StackTrace}\n");
                    }
                    if (r.HasChildren)
                    {
                        foreach (var c in r.Children) CollectFailures(c);
                    }
                }
                CollectFailures(result);

                File.WriteAllText("TestResults.txt", sb.ToString());
            }
        }

        [MenuItem("Tools/Run All Tests")]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
            Debug.Log("EditMode tests started via TestRunnerApi...");
        }

        [MenuItem("Tools/Run PlayMode Tests")]
        public static void RunPlayMode()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
            Debug.Log("PlayMode tests started via TestRunnerApi...");
        }
    }
}

//
// NUnitTestSuite.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using MonoDevelop.UnitTesting.NUnit.External;

namespace MonoDevelop.UnitTesting.NUnit
{
	public class NUnitTestSuite: UnitTestGroup
	{
		NunitTestInfo testInfo;
		NUnitAssemblyTestSuite rootSuite;
		string fullName;
		
		UnitTestCollection childNamespaces;

		public NUnitTestSuite (NUnitAssemblyTestSuite rootSuite, NunitTestInfo tinfo): base (tinfo.Name)
		{
			fullName = !string.IsNullOrEmpty (tinfo.PathName) ? tinfo.PathName + "." + tinfo.Name : tinfo.Name;
			this.testInfo = tinfo;
			this.rootSuite = rootSuite;
			this.TestSourceCodeDocumentId = this.TestId = tinfo.TestId;
			this.childNamespaces = new UnitTestCollection ();
		}

		public override bool HasTests {
			get {
				return true;
			}
		}

		public UnitTestCollection ChildNamespaces {
			get {
				return childNamespaces;
			}
		}
		
		public string ClassName {
			get {
				return fullName;
			}
		}
		
		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return rootSuite.RunUnitTest (this, fullName, fullName, null, testContext);
		}
		
		protected override bool OnCanRun (MonoDevelop.Core.Execution.IExecutionHandler executionContext)
		{
			return rootSuite.CanRun (executionContext);
		}

		
		protected override void OnCreateTests ()
		{
			if (testInfo.Tests == null)
				return;

			foreach (NunitTestInfo test in testInfo.Tests) {
				if (test.Tests != null) {
					var newTest = new NUnitTestSuite (rootSuite, test);
					newTest.FixtureTypeName = test.FixtureTypeName;
					newTest.FixtureTypeNamespace = test.FixtureTypeNamespace;

					ChildStatus (test, out bool isNamespace, out bool hasClassAsChild);

					if (isNamespace) {
						var forceLoad = newTest.Tests;
						foreach (var child in newTest.ChildNamespaces) {
							child.Title = newTest.Title + "." + child.Title;
							childNamespaces.Add (child);
						}
						if (hasClassAsChild) {
							childNamespaces.Add (newTest);
						}
					} else {
						Tests.Add (newTest);
					}
				} else {
					var newTest = new NUnitTestCase (rootSuite, test, ClassName);
					newTest.FixtureTypeName = test.FixtureTypeName;
					newTest.FixtureTypeNamespace = test.FixtureTypeNamespace;
					Tests.Add (newTest);
				}
			}
		}

		public void ChildStatus (NunitTestInfo test, out bool isNamespace, out bool hasClassAsChild)
		{
			isNamespace = false;
			hasClassAsChild = false;
			foreach (NunitTestInfo child in test.Tests) {
				if (child.Tests != null) {
					isNamespace = true;
					if (child.Tests [0].Tests == null)
						hasClassAsChild = true;
				}
			}
		}
		
		public override SourceCodeLocation SourceCodeLocation {
			get {
				UnitTest p = Parent;
				while (p != null) {
					NUnitAssemblyTestSuite root = p as NUnitAssemblyTestSuite;
					if (root != null)
						return root.GetSourceCodeLocation (this);
					p = p.Parent;
				}
				return null; 
			}
		}
	}
}


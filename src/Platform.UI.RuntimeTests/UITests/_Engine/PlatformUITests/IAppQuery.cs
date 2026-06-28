#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UITest.Helpers.Queries;

namespace CodeBrix.Platform.UITest; //Was previously: Uno.UITest

public interface IAppQuery
{
	QueryEx All();
	QueryEx Marked(string marked);

	internal IEnumerable<QueryResult> Execute(IEnumerable<QueryResult> elements);
}

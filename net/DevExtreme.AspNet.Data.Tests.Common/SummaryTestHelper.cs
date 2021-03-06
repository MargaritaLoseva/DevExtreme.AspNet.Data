﻿using DevExtreme.AspNet.Data.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace DevExtreme.AspNet.Data.Tests {

    public static class SummaryTestHelper {

        public interface IEntity {
            string Group1 { get; set; }
            string Group2 { get; set; }
            int? Value { get; set; }
        }

        public static IEnumerable<T> GenerateTestData<T>(Func<T> itemFactory) where T : IEntity {
            T CreateItem(string group1, string group2, int? value) {
                var item = itemFactory();
                item.Group1 = group1;
                item.Group2 = group2;
                item.Value = value;
                return item;
            }

            return new[] {
                CreateItem("A", "A", null),
                CreateItem("A", "A", 1),
                CreateItem("A", "B", 3),
                CreateItem("A", "B", 5),
                CreateItem("B", "A", null),
            };
        }

        #warning Remove summaryTypes with https://github.com/aspnet/EntityFrameworkCore/issues/11711 fix
        public static void Run<T>(IQueryable<T> data, string[] summaryTypes = null) where T : IEntity {
            summaryTypes = summaryTypes ?? new[] { "count", "min", "max", "sum", "avg" };

            var group = Array.ConvertAll(
                new[] { nameof(IEntity.Group1), nameof(IEntity.Group2) },
                i => new GroupingInfo {
                    Selector = i,
                    IsExpanded = false
                }
            );

            var summary = Array.ConvertAll(
                summaryTypes,
                i => new SummaryInfo {
                    Selector = nameof(IEntity.Value),
                    SummaryType = i
                }
            );

            var loadOptions = new SampleLoadOptions {
                Group = group,
                GroupSummary = summary,
                TotalSummary = summary
            };

            object[] CreateExpectation(int count, int? min, int? max, decimal? sum, decimal? avg) {
                var list = new List<object>();
                foreach(var i in summaryTypes) {
                    switch(i) {
                        case "count":
                            list.Add(count);
                            break;
                        case "min":
                            list.Add(min);
                            break;
                        case "max":
                            list.Add(max);
                            break;
                        case "sum":
                            list.Add(sum);
                            break;
                        case "avg":
                            list.Add(avg);
                            break;
                    }
                }
                return list.ToArray();
            }

            {
                var loadResult = DataSourceLoader.Load(data, loadOptions);
                var rootItems = (IList<Group>)loadResult.data;

                var group_A = rootItems[0];
                var group_B = rootItems[1];

                var group_A_A = (Group)group_A.items[0];
                var group_A_B = (Group)group_A.items[1];
                var group_B_A = (Group)group_B.items[0];

                Assert.Equal(CreateExpectation(4, 1, 5, 9m, 3m), group_A.summary);
                Assert.Equal(CreateExpectation(2, 1, 1, 1m, 1m), group_A_A.summary);
                Assert.Equal(CreateExpectation(2, 3, 5, 8m, 4m), group_A_B.summary);

                Assert.Equal(CreateExpectation(1, null, null, 0m, null), group_B.summary);
                Assert.Equal(CreateExpectation(1, null, null, 0m, null), group_B_A.summary);

                Assert.Equal(CreateExpectation(5, 1, 5, 9m, 3m), loadResult.summary);
            }

            loadOptions.Filter = new[] { nameof(IEntity.Group1), "nonexistent" };

            {
                var loadResult = DataSourceLoader.Load(data, loadOptions);
                Assert.Equal(CreateExpectation(0, null, null, 0m, null), loadResult.summary);
            }
        }

        /*

            A           Count=4, Min=1, Max=5, Sum=9, Avg=3
                A       Count=2, Min=1, Max=1, Sum=1, Avg=1
                    N
                    1
                B       Count=2, Min=3, Max=5, Sum=8, Avg=4
                    3
                    5
            B           Count=1, Min=N, Max=N, Sum=0*, Avg=N
                A       Count=1, Min=N, Max=N, Sum=0*, Avg=N
                    N

            TOTALS:     Count=5, Min=1, Max=5, Sum=9, Avg=3
            * - see SumFix
        */

    }

}

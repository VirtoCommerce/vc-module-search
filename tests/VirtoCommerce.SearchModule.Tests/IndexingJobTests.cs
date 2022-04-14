using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Data.BackgroundJobs;
using Xunit;

namespace VirtoCommerce.SearchModule.Tests;

public class IndexingJobTests
{

    [Theory]
    [ClassData(typeof(GroupingTestData))]
    public void GroupingTests(List<IndexEntry> sourceEntries, IEnumerable<IGrouping<string, IndexEntry>> expectGroupedEntries)
    {
        // Arrange

        // Act
        var result = IndexingJobs.GetGroupedByTypeAndDistinctedByChangeTypeIndexEntries(sourceEntries).ToList();

        // Assert
        foreach (var expectGroupedEntry in expectGroupedEntries)
        {
            var resultEntries = result.First(x => x.Key == expectGroupedEntry.Key).Select(x => x).ToList();
            Assert.Equal(expectGroupedEntry.ToList(), resultEntries, new IndexEntriesEqualityComparer());
        }
    }

    public class GroupingTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new List<IndexEntry>
                {
                    new()
                    {
                        EntryState = EntryState.Added,
                        Id = "testId1",
                        Type = "testType1",
                    },
                    new()
                    {
                        EntryState = EntryState.Added,
                        Id = "testId1",
                        Type = "testType2",
                    },new()
                    {
                        EntryState = EntryState.Modified,
                        Id = "testId1",
                        Type = "testType1",
                    },
                    new()
                    {
                        EntryState = EntryState.Deleted,
                        Id = "testId1",
                        Type = "testType2",
                    }
                },
                new List<IndexEntry>
                {
                    new()
                    {
                        EntryState = EntryState.Added,
                        Id = "testId1",
                        Type = "testType1",
                    },
                    new()
                    {
                        EntryState = EntryState.Deleted,
                        Id = "testId1",
                        Type = "testType2",
                    },
                }.GroupBy(x => x.Type)
            };
            yield return new object[]
            {
                new List<IndexEntry>
                {
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Added,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = null,
                        EntryState = EntryState.Added,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Modified,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Modified,
                        Type = "testType2",
                    },
                },
                new List<IndexEntry>
                {
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Added,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Modified,
                        Type = "testType2",
                    },
                }.GroupBy(x => x.Type),
            };
            yield return new object[]
            {
                new List<IndexEntry>
                {
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Added,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Modified,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Deleted,
                        Type = "testType1",
                    }
                },
                new List<IndexEntry>
                {
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Deleted,
                        Type = "testType1",
                    }
                }.GroupBy(x => x.Type),
            };
            yield return new object[]
            {
                new List<IndexEntry>
                {
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Added,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Modified,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId2",
                        EntryState = EntryState.Modified,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Deleted,
                        Type = "testType2",
                    },
                },
                new List<IndexEntry>
                {
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Added,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId2",
                        EntryState = EntryState.Modified,
                        Type = "testType1",
                    },
                    new()
                    {
                        Id = "testId1",
                        EntryState = EntryState.Deleted,
                        Type = "testType2",
                    },
                }.GroupBy(x => x.Type),
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class IndexEntriesEqualityComparer : EqualityComparer<IndexEntry>
    {
        public override bool Equals(IndexEntry x, IndexEntry y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }

            return x.Type == y.Type && x.Id == y.Id && x.EntryState == y.EntryState;
        }

        public override int GetHashCode(IndexEntry obj)
        {
            return $"{obj.Type}.{obj.Id}".GetHashCode();
        }
    }
}


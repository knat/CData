using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CData;
using Example.Business;
using Example.Business.API;

[assembly: ContractNamespace("http://example.com/business", "Example.Business")]
[assembly: ContractNamespace("http://example.com/business/api", "Example.Business.API")]

class Program {
    static void Main() {
        DataSet ds = new DataSet {
            PersonMap = new Dictionary<int, Person> {
                {1, new Customer { Id = 1, Name = "Tank", RegDate = DateTimeOffset.Now, OrderList = new List<Order> {
                            new Order { Amount = 436.99M, IsUrgent = true},
                            new Order { Amount = 98.77M, IsUrgent = false},
                        }
                    }
                },
                {2, new Customer { Id = 2, Name = "Mike", RegDate = DateTimeOffset.UtcNow, OrderList = null } },
                {3, new Supplier { Id = 3, Name = "Eric", RegDate = DateTimeOffset.UtcNow - TimeSpan.FromHours(543), 
                        BankAccount="11223344", ProductIdSet = new HashSet<int> {1, 3, 7} }
                },
            }
        };
        using (var writer = new StreamWriter("DataSet.txt")) {
            ds.Save(writer, "    ", "\r\n");
        }
        //
        DataSet result;
        var diagCtx = new DiagContext();
        using (var reader = new StreamReader("DataSet.txt")) {
            if (!DataSet.TryLoad("DataSet.txt", reader, diagCtx, out result)) {
                foreach (var diag in diagCtx) {
                    Console.WriteLine(diag.ToString());
                }
                Debug.Assert(false);
            }
        }

    }
}


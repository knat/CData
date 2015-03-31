//Biz.cdc
namespace "http://example.com/business"
{
    class Person[abstract]
    {
        Id as Int32
        Name as String
        RegDate as DateTimeOffset
    }

    class Customer extends Person
    {
        Reputation as Reputation
        OrderList as nullable<list<Order>>
    }

    enum Reputation as Int32
    {
        None = 0
        Bronze = 1
        Silver = 2
        Gold = 3
        Bad = -1
    }

    class Order
    {
        Amount as Decimal
        IsUrgent as Boolean
    }

    class Supplier extends Person
    {
        BankAccount as String
        ProductIdSet as set<Int32>
    }
}

namespace "http://example.com/business/api"
{
    import "http://example.com/business" as biz

    class DataSet
    {
        PersonMap as map<Int32, biz:Person>
    }
}
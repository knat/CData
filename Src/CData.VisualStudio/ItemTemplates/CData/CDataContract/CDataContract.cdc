//Visit https://github.com/knat/CData for more information.

namespace "http://example.com/project1"
{
    class Class1
    {
        Property1 as Int32
        Property2 as String
    }

}

namespace "http://example.com/project2"
{
    import "http://example.com/project1" as p1
    class Class2
    {
        Property1 as Boolean
        Property2 as DateTimeOffset
        Property3 as list<p1:Class1>
    }
}

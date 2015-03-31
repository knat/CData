//Visit https://github.com/knat/CData for more information.

namespace "http://example.com/project1"
{
    class Class1
    {
        Id as Int32
    }
}

namespace "http://example.com/project2"
{
    import "http://example.com/project1" as p1

    class Class2 extends p1:Class1
    {
    }
}

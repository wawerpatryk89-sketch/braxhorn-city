import unittest
from check_csharp_scopes import check_source


class ScopeTests(unittest.TestCase):
    def errors(self, body, parameters=''):
        return check_source('class A { void M(' + parameters + ') {' + body + '} }')

    def test_city90_nested_position_then_outer_waypoint_id(self):
        self.assertTrue(self.errors('foreach(int su in signs) { for(int k=0;k<2;k++){ var wc = Pos(); Draw(wc); } string wc = "arena_wc"; Use(wc); }'))

    def test_city90_renamed_waypoint_preserves_position(self):
        self.assertFalse(self.errors('foreach(int su in signs) { for(int k=0;k<2;k++){ var wc = Pos(); Draw(wc); } string wcId = "arena_wc"; Use(wcId); }'))

    def test_nested_local_before_or_after_outer_is_invalid(self):
        for s in ['int x=0; {int x=1;}', '{int x=1;} int x=0;']:
            self.assertTrue(self.errors(s))

    def test_sibling_blocks_and_loops_can_reuse_names(self):
        self.assertFalse(self.errors('{int x=0;} {int x=1;} for(int i=0;i<1;i++){} for(int i=0;i<1;i++){}'))

    def test_duplicate_in_same_block_is_invalid(self):
        self.assertTrue(self.errors('int x=0; int x=1;'))

    def test_parameter_cannot_be_redeclared_in_body(self):
        self.assertTrue(self.errors('{int arg=0;}', 'int arg'))

    def test_loop_variable_cannot_be_redeclared_in_body(self):
        self.assertTrue(self.errors('for(int i=0;i<1;i++){int i=1;}'))
        self.assertTrue(self.errors('foreach(var item in items){int item=1;}'))

    def test_catch_variable_has_own_scope(self):
        self.assertTrue(self.errors('try{}catch(System.Exception err){int err=1;}'))
        self.assertFalse(self.errors('try{}catch(System.Exception err){} try{}catch(System.Exception err){}'))

    def test_using_variable_cannot_be_redeclared_inside(self):
        self.assertTrue(self.errors('using(var resource=Get()){int resource=1;}'))

    def test_member_names_are_not_local_declarations(self):
        self.assertFalse(check_source('class A { int x; void M(){int x=1;} void N(){int x=2;} }'))

    def test_lambda_and_local_function_use_independent_spaces(self):
        self.assertFalse(self.errors('int x=0; var f=(int x)=>x; void Local(int x){Use(x);}'))
        self.assertTrue(self.errors('var f=(int x)=>{int x=1;return x;};'))

    def test_strings_comments_and_escaped_names(self):
        self.assertFalse(self.errors('int x=0; string text="{int x=1;}"; /* int x=1; */'))
        self.assertTrue(self.errors('int @x=0; {int x=1;}'))

    def test_conditional_compilation_is_deferred_to_unity(self):
        self.assertFalse(self.errors('\n#if ANDROID\nint x=0;\n#else\nint x=1;\n#endif\n'))

    def test_malformed_source_is_not_accepted(self):
        self.assertTrue(check_source('class A { void M( {'))


if __name__ == '__main__':
    unittest.main()

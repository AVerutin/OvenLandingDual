namespace OvenLanding.Data
{
    public class DbQueries
    {
        private string _returnsByMelt =
            "set session statement_timeout  to '{0}ms'; " +
            "with t0 as (select rp.unit_id_parent as id_posad, pr.value_s as prod, " +
            "l.tm_beg::timestamp as tm_beg, l.tm_end::timestamp as tm_end, " +
            "l.unit_id as unit_dt, t.unit_id as unit_task, pm.value_s as melt, " +
            "t.pos, pc.value_s as count, t.date_reg::timestamp as date_reg, " +
            "p.value_n as billet_w, pw.value_n as coil_w, " +
            "l.node_id, n.node_code, l.id " +
            "from mts.locations l join mts.nodes n on n.id = l.node_id " +
            "join mts.units u on u.id= l.unit_id " +
            "left join mts.units_relations r on r.unit_id_child = u.id " +
            "left join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 20100 " +
            "left join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent " +
            "left join mts.passport pm on pm.unit_id = rp.unit_id_parent and pm.param_id=10000001 " +
            "left join mts.passport pc on pc.unit_id = rp.unit_id_parent and pc.param_id=10000004 " +
            "left join mts.passport pr on pr.unit_id = rp.unit_id_parent and pr.param_id=1 " +
            "left join mts.passport p on p.unit_id = l.unit_id and p.param_id=1000 " +
            "left join mts.passport pw on pw.unit_id = t.unit_id and pw.param_id=10000014 " +
            "where l.node_id = 50100 and pm.value_s='{1}' order by node_id, l.tm_beg) " +
            "select melt, tm_beg, tm_end, pos, count, date_reg, billet_w, node_code, node_id " +
            "from t0 group by melt, tm_beg, tm_end, pos, count, date_reg, billet_w, node_code, node_id order by tm_beg;";

        private string _getWeightedIngotsCount =
            "set session statement_timeout  to '{0}ms'; " +
            "select rp.unit_id_parent id_posad, " +
            "pm.value_s melt, " +
            "pc.value_n count_posad, " +
            "count(distinct l.unit_id) count_dt " +
            "from mts.locations l join mts.units_relations r on r.unit_id_child = l.unit_id " +
            "join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 10000 " +
            "join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent and rp.unit_id_parent = {1} " +
            "join mts.passport pm on pm.unit_id = rp.unit_id_parent and pm.param_id=10000001 " +
            "join mts.passport pc on pc.unit_id = rp.unit_id_parent and pc.param_id=10000004 " +
            "join mts.passport p on p.unit_id = l.unit_id and p.param_id = 1000 " +
            "where l.node_id = 20100 group by rp.unit_id_parent,pm.value_s,pc.value_n;";

        private string _getIngotOnScales =
            "SELECT l.unit_id, t.pos, l.tm_beg, p1.value_s billet_weight, p22.value_s billet_num, rp.unit_id_parent id_posad," +
            "p19.value_s as melt, p2.value_s as steel_grade, p3.value_s as section, p4.value_s as count, p5.value_s as weight_all, " +
            "p6.value_s as weight_one, p7.value_s as length, p9.value_s as gost, p10.value_s as diameter, p11.value_s as customer, " +
            "p12.value_s as shift, p13.value_s as class, p15.value_s as prod_code, p18.value_s as profile, " +
            "(select count(distinct ps.unit_id) from mts.passport ps join mts.units_relations rb on rb.unit_id_child = ps.unit_id " +
            "join mts.units_relations rbb on rbb.unit_id_child = rb.unit_id_parent where rbb.unit_id_parent = rp.unit_id_parent " +
            "and ps.param_id = 1000 and ps.project_id = 1):: numeric as c_count_weight FROM mts.locations l " +
            "join mts.units ul on ul.id = l.unit_id and ul.unit_type_id is null and ul.project_id = 1 " +
            "left join mts.units_relations r on r.unit_id_child = l.unit_id " +
            "left join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent " +
            "left join mts.units up on up.id = rp.unit_id_parent and up.unit_type_id = 10000001 and up.project_id = 1 " +
            "left join mts.unit_tasks t on t.unit_id = r.unit_id_parent " +
            "left join mts.passport p1 on p1.unit_id = l.unit_id and p1.project_id = 1 and p1.param_date >= ul.date_reg and p1.param_id = 1000 " +
            "left join mts.passport p22 on p22.unit_id = l.unit_id and p22.project_id = 1 and p22.param_date >= ul.date_reg and p22.param_id = 10000022 " +
            "left join mts.passport p2 on p2.unit_id = up.id and p2.project_id = 1 and p2.param_date >= up.date_reg and p2.param_id = 10000002 " +
            "left join mts.passport p3 on p3.unit_id = up.id and p3.project_id = 1 and p3.param_date >= up.date_reg and p3.param_id = 10000003 " +
            "left join mts.passport p4 on p4.unit_id = up.id and p4.project_id = 1 and p4.param_date >= up.date_reg and p4.param_id = 10000004 " +
            "left join mts.passport p5 on p5.unit_id = up.id and p5.project_id = 1 and p5.param_date >= up.date_reg and p5.param_id = 10000005 " +
            "left join mts.passport p6 on p6.unit_id = up.id and p6.project_id = 1 and p6.param_date >= up.date_reg and p6.param_id = 10000006 " +
            "left join mts.passport p7 on p7.unit_id = up.id and p7.project_id = 1 and p7.param_date >= up.date_reg and p7.param_id = 10000005 " +
            "left join mts.passport p9 on p9.unit_id = up.id and p9.project_id = 1 and p9.param_date >= up.date_reg and p9.param_id = 10000006 " +
            "left join mts.passport p10 on p10.unit_id = up.id and p10.project_id = 1 and p10.param_date >= up.date_reg and p10.param_id = 10000010 " +
            "left join mts.passport p11 on p11.unit_id = up.id and p11.project_id = 1 and p11.param_date >= up.date_reg and p11.param_id = 10000011 " +
            "left join mts.passport p12 on p12.unit_id = up.id and p12.project_id = 1 and p12.param_date >= up.date_reg and p12.param_id = 10000012 " +
            "left join mts.passport p13 on p13.unit_id = up.id and p13.project_id = 1 and p13.param_date >= up.date_reg and p13.param_id = 10000013 " +
            "left join mts.passport p15 on p15.unit_id = up.id and p15.project_id = 1 and p15.param_date >= up.date_reg and p15.param_id = 10000015 " +
            "left join mts.passport p18 on p18.unit_id = up.id and p18.project_id = 1 and p18.param_date >= up.date_reg and p18.param_id = 10000018 " +
            "left join mts.passport p19 on p19.unit_id = up.id and p19.project_id = 1 and p19.param_date >= up.date_reg and p19.param_id = 10000001 " +
            "WHERE l.tm_end is null and l.node_id = 20100 ORDER BY l.id;";

        // private string _getIngotsInOven =
        //     "select coalesce((select min(distinct l3.tm_beg) from mts.locations l3 join mts.nodes n3 on n3.id = l3.node_id " +
        //     "join mts.units_relations r3 on r3.unit_id_child = l3.unit_id join mts.units_relations rp5 on rp5.unit_id_child = r3.unit_id_parent " +
        //     "where n3.thread_id = 3 and rp.unit_id_parent = rp5.unit_id_parent and l3.tm_beg >= u.date_reg), min(l.tm_beg))  beg_kiln, " +
        //     "coalesce(rp.unit_id_parent, l.unit_id) as posad_id, p1.value_s as melt, p4.value_n as count_posad, " +
        //     "p2.value_s as steel_grade, p3.value_s as section, p10.value_s as diameter, p11.value_s as customer, " +
        //     "p18.value_s as profile, count(distinct l.unit_id) count_kiln, (select count(distinct unit_id) " +
        //     "from mts.locations l5 join mts.nodes n5 on n5.id = l5.node_id join mts.units_relations r5 on r5.unit_id_child = l5.unit_id " +
        //     "join mts.units_relations rp5 on rp5.unit_id_child = r5.unit_id_parent where n5.thread_id = 5 " +
        //     "and rp.unit_id_parent = rp5.unit_id_parent and l5.tm_beg >= u.date_reg and exists (select * from mts.locations l3 join mts.nodes n3 on n3.id = l3.node_id " +
        //     "where l3.unit_id = l5.unit_id and n3.thread_id = 3 and l3.tm_beg >= u.date_reg)) count_out_kiln, " +
        //     "(select count(distinct ps.unit_id) from mts.passport ps join mts.units_relations rb on rb.unit_id_child = ps.unit_id " +
        //     "join mts.units_relations rbb on rbb.unit_id_child = rb.unit_id_parent where rbb.unit_id_parent = rp.unit_id_parent " +
        //     "and ps.param_id = 1000 and ps.project_id = 1):: numeric as c_count_weight, p15.value_n as prod_code " +
        //     "from mts.locations l join mts.nodes n on n.id = l.node_id and n.thread_id = 3 " +
        //     "left join mts.units_relations r on r.unit_id_child = l.unit_id " +
        //     "left join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent " +
        //     "left join mts.units u on u.id = rp.unit_id_parent and u.unit_type_id = 10000001 and u.project_id = 1 " +
        //     "left join mts.passport p2 on p2.unit_id = u.id and p2.project_id = 1 and p2.param_date >= u.date_reg and p2.param_id = 10000002 " +
        //     "left join mts.passport p3 on p3.unit_id = u.id and p3.project_id = 1 and p3.param_date >= u.date_reg and p3.param_id = 10000003 " +
        //     "left join mts.passport p10 on p10.unit_id = u.id and p10.project_id = 1 and p10.param_date >= u.date_reg and p10.param_id = 10000010 " +
        //     "left join mts.passport p11 on p11.unit_id = u.id and p11.project_id = 1 and p11.param_date >= u.date_reg and p11.param_id = 10000011 " +
        //     "left join mts.passport p18 on p18.unit_id = u.id and p18.project_id = 1 and p18.param_date >= u.date_reg and p18.param_id = 10000018 " +
        //     "left join mts.passport p15 on p15.unit_id = u.id and p15.project_id = 1 and p15.param_date >= u.date_reg and p15.param_id = 10000015 " +
        //     "left join mts.passport p1 on p1.unit_id = rp.unit_id_parent and p1.param_id = 10000001 and p1.param_date >= u.date_reg and p1.project_id = 1 " +
        //     "left join mts.passport p4 on p4.unit_id = rp.unit_id_parent and p4.param_id = 10000004 and p4.param_date >= u.date_reg and p4.project_id = 1 " +
        //     "where l.tm_end is null and l.tm_beg >= u.date_reg " +
        //     "group by rp.unit_id_parent, coalesce(rp.unit_id_parent, l.unit_id), u.date_reg, p1.value_s, p4.value_n, p2.value_s, p3.value_s,  p10.value_s, p11.value_s, p18.value_s, p15.value_n " +
        //     "order by 1 desc;";

        private string _getIngotsInOvenNew =
            "SELECT beg_kiln, posad_id, max(case when pp.param_id = 10000001 then pp.value_s end) as melt, max(case when pp.param_id = 10000004 then pp.value_s end) as count_posad, " +
            "max(case when pp.param_id = 10000002 then pp.value_s end) as steel_grade, max(case when pp.param_id = 10000003 then pp.value_s end) as section, " +
            "max(case when pp.param_id = 10000010 then pp.value_s end) as diameter, max(case when pp.param_id = 10000011 then pp.value_s end) as customer, " +
            "max(case when pp.param_id = 10000018 then pp.value_s end) as profile, count_kiln, (select count(distinct unit_id) " +
            "from mts.locations l5 join mts.nodes n5 on n5.id = l5.node_id and n5.thread_id = 5 join mts.units_relations r5 on r5.unit_id_child = l5.unit_id " +
            "join mts.units_relations rp5 on rp5.unit_id_child = r5.unit_id_parent where posad_id = rp5.unit_id_parent) count_out_kiln, " +
            "count_weight, max(case when pp.param_id = 10000015 then pp.value_s end) as prod_code, start_pos, end_pos FROM (" +
            "select min(tm_beg) as beg_kiln, min(gr1) as sort, posad_id, part_id, max(unit_part) as unit_part, gr1 - gr2 as gr, " +
            "count(distinct k.unit_id) as count_kiln, count(distinct k.unit_w) as count_weight, max(mes_id) as part_unit_id, " +
            "min(pos) as start_pos, max(pos) as end_pos from (select l.unit_id, l.tm_beg, r.unit_id_parent as mes_id, rp.unit_id_parent as posad_id, " +
            "p.value_n as part_id, t.pos, row_number() over (order by l.node_id desc,tm_beg,l.id) as gr1, " +
            "row_number() over (partition by rp.unit_id_parent,p.value_n order by l.node_id desc,tm_beg,l.id) as gr2, " +
            "pw.unit_id as unit_w, p.unit_id as unit_part from mts.locations l join mts.nodes n on n.id = l.node_id and n.thread_id = 3 " +
            "left join mts.units_relations r on r.unit_id_child = l.unit_id left join mts.unit_tasks t on t.unit_id = r.unit_id_parent and t.node_id = 10000 " +
            "left join mts.units_relations rp on rp.unit_id_child = r.unit_id_parent left join mts.passport pw on pw.unit_id = l.unit_id and pw.param_id = 1000 " +
            "left join mts.passport p on p.unit_id = r.unit_id_parent and p.param_id = 10000025 where l.tm_end is null) k " +
            "group by posad_id, part_id, gr1 - gr2) a left join mts.passport pp on pp.unit_id = coalesce(unit_part,posad_id) " +
            "GROUP BY beg_kiln, posad_id, count_kiln, count_weight, sort, start_pos, end_pos ORDER BY sort desc;";

        private string _getMeltParams =
            "select p.param_id, pp.param_name, p.value_n, p.value_s from mts.passport p join passport_param pp on pp.id=p.param_id and pp.project_id=p.project_id where unit_id={0};";
        
        /// <summary>
        /// Получить запрос на получение списка возвратов по номеру плавки
        /// </summary>
        /// <param name="melt">Номер плавки</param>
        /// <param name="timeout">Максимальное время выполнения запроса в мс</param>
        /// <returns>Запрос на получение списка возвратов по номеру плавки</returns>
        public string GetReturnsByMelt(string melt, int timeout)
        {
            return string.Format(_returnsByMelt, timeout, melt);
        }

        /// <summary>
        /// Получить запрос на количество взвешенных заготовок по иденту посада
        /// </summary>
        /// <param name="meltNumber">Идент посада</param>
        /// <param name="timeout">Максимальное время выполнения запроса в мс</param>
        /// <returns>Запрос на количество взвешенных заготовок по иденту посада</returns>
        public string GetWeightedIngotsCount(int meltNumber, int timeout) =>
            string.Format(_getWeightedIngotsCount, timeout, meltNumber);

        /// <summary>
        /// Получить запрос на список заготовок на весах
        /// </summary>
        public string GetIngotOnScales => _getIngotOnScales;

        /// <summary>
        /// Получить запрос на список посадов в печи
        /// </summary>
        public string GetIngotsInOven => _getIngotsInOvenNew;

        /// <summary>
        /// Получить запрос на получение всех парметров посада
        /// </summary>
        /// <param name="uid">Идент посада</param>
        /// <returns>Запрос на получение параметров посада</returns>
        public string GetMeltParams(int uid) => string.Format(_getMeltParams, uid);
    }
}
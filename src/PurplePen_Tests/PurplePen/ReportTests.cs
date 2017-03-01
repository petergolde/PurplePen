/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    using PurplePen.MapModel;

    [TestClass]
    public class ReportTests
    {
        UndoMgr undomgr;
        EventDB eventDB;

        public void Setup(string basename)
        {
            undomgr = new UndoMgr(10);
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile(basename));
            eventDB.Validate();
        }

        [TestMethod]
        public void CourseLoad1()
        {
            Setup(@"reports\marymoor2.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateLoadReport(eventDB);

            Assert.AreEqual(@"
  <h1>Competitor Load Summary for Marymoor WIOL 2</h1>
  <p>
    <strong>WARNING:</strong> Some or all courses do not have competitor loads set for them. The loads listed below may be incorrect or missing for this reason. To set competitor loads, select ""Competitor Load"" from the ""Course"" menu.</p>
  <h2>Control load</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Control</th>
      <th># Courses</th>
      <th>Load</th>
    </tr>
    <tr>
      <td>48</td>
      <td>5</td>
      <td>229</td>
    </tr>
    <tr>
      <td>51</td>
      <td>5</td>
      <td>229</td>
    </tr>
    <tr>
      <td>57</td>
      <td>5</td>
      <td>229</td>
    </tr>
    <tr>
      <td>79</td>
      <td>5</td>
      <td>229</td>
    </tr>
    <tr>
      <td>41</td>
      <td>5</td>
      <td>228</td>
    </tr>
    <tr>
      <td>47</td>
      <td>5</td>
      <td>219</td>
    </tr>
    <tr>
      <td>39</td>
      <td>4</td>
      <td>193</td>
    </tr>
    <tr>
      <td>38</td>
      <td>6</td>
      <td>184</td>
    </tr>
    <tr>
      <td>45</td>
      <td>4</td>
      <td>174</td>
    </tr>
    <tr>
      <td>56</td>
      <td>3</td>
      <td>166</td>
    </tr>
    <tr>
      <td>50</td>
      <td>5</td>
      <td>152</td>
    </tr>
    <tr>
      <td>36</td>
      <td>4</td>
      <td>152</td>
    </tr>
    <tr>
      <td>46</td>
      <td>4</td>
      <td>152</td>
    </tr>
    <tr>
      <td>77</td>
      <td>3</td>
      <td>147</td>
    </tr>
    <tr>
      <td>43</td>
      <td>4</td>
      <td>142</td>
    </tr>
    <tr>
      <td>59</td>
      <td>4</td>
      <td>142</td>
    </tr>
    <tr>
      <td>78</td>
      <td>4</td>
      <td>133</td>
    </tr>
    <tr>
      <td>54</td>
      <td>3</td>
      <td>125</td>
    </tr>
    <tr>
      <td>52</td>
      <td>2</td>
      <td>121</td>
    </tr>
    <tr>
      <td>37</td>
      <td>3</td>
      <td>116</td>
    </tr>
    <tr>
      <td>40</td>
      <td>3</td>
      <td>107</td>
    </tr>
    <tr>
      <td>72</td>
      <td>4</td>
      <td>97</td>
    </tr>
    <tr>
      <td>35</td>
      <td>2</td>
      <td>89</td>
    </tr>
    <tr>
      <td>42</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>55</td>
      <td>4</td>
      <td>71</td>
    </tr>
    <tr>
      <td>70</td>
      <td>4</td>
      <td>71</td>
    </tr>
    <tr>
      <td>76</td>
      <td>4</td>
      <td>71</td>
    </tr>
    <tr>
      <td>49</td>
      <td>3</td>
      <td>71</td>
    </tr>
    <tr>
      <td>60</td>
      <td>2</td>
      <td>71</td>
    </tr>
    <tr>
      <td>53</td>
      <td>4</td>
      <td>70</td>
    </tr>
    <tr>
      <td>71</td>
      <td>3</td>
      <td>70</td>
    </tr>
    <tr>
      <td>75</td>
      <td>3</td>
      <td>70</td>
    </tr>
    <tr>
      <td>80</td>
      <td>2</td>
      <td>70</td>
    </tr>
    <tr>
      <td>44</td>
      <td>3</td>
      <td>44</td>
    </tr>
    <tr>
      <td>58</td>
      <td>2</td>
      <td>44</td>
    </tr>
    <tr>
      <td>74</td>
      <td>2</td>
      <td>44</td>
    </tr>
    <tr>
      <td>31</td>
      <td>1</td>
      <td />
    </tr>
    <tr>
      <td>73</td>
      <td>1</td>
      <td />
    </tr>
  </table>
  <h2>Leg load</h2>
  <p>(only legs used by more than one course appear in the following table)</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th># Courses</th>
      <th>Load</th>
    </tr>
    <tr>
      <td>38–Finish</td>
      <td>6</td>
      <td>184</td>
    </tr>
    <tr>
      <td>47–48</td>
      <td>2</td>
      <td>122</td>
    </tr>
    <tr>
      <td>48–50</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>57–79</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>Start–59</td>
      <td>2</td>
      <td>71</td>
    </tr>
    <tr>
      <td>55–38</td>
      <td>2</td>
      <td />
    </tr>
  </table>

", result);
        }

        [TestMethod]
        public void CourseLoad2()
        {
            Setup(@"reports\marymoor3.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateLoadReport(eventDB);

            string expected = @"
  <h1>Competitor Load Summary for Marymoor WIOL 2</h1>
  <h2>Control load</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Control</th>
      <th># Courses</th>
      <th>Load</th>
    </tr>
    <tr>
      <td>38</td>
      <td>6</td>
      <td>227</td>
    </tr>
    <tr>
      <td>41</td>
      <td>4</td>
      <td>184</td>
    </tr>
    <tr>
      <td>48</td>
      <td>4</td>
      <td>184</td>
    </tr>
    <tr>
      <td>51</td>
      <td>3</td>
      <td>158</td>
    </tr>
    <tr>
      <td>57</td>
      <td>3</td>
      <td>158</td>
    </tr>
    <tr>
      <td>79</td>
      <td>3</td>
      <td>158</td>
    </tr>
    <tr>
      <td>47</td>
      <td>3</td>
      <td>148</td>
    </tr>
    <tr>
      <td>39</td>
      <td>2</td>
      <td>122</td>
    </tr>
    <tr>
      <td>56</td>
      <td>2</td>
      <td>122</td>
    </tr>
    <tr>
      <td>50</td>
      <td>3</td>
      <td>110</td>
    </tr>
    <tr>
      <td>45</td>
      <td>2</td>
      <td>103</td>
    </tr>
    <tr>
      <td>77</td>
      <td>2</td>
      <td>103</td>
    </tr>
    <tr>
      <td>36</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>42</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>46</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>54</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>52</td>
      <td>1</td>
      <td>77</td>
    </tr>
    <tr>
      <td>43</td>
      <td>2</td>
      <td>71</td>
    </tr>
    <tr>
      <td>59</td>
      <td>2</td>
      <td>71</td>
    </tr>
    <tr>
      <td>53</td>
      <td>3</td>
      <td>69</td>
    </tr>
    <tr>
      <td>78</td>
      <td>2</td>
      <td>62</td>
    </tr>
    <tr>
      <td>72</td>
      <td>2</td>
      <td>55</td>
    </tr>
    <tr>
      <td>75</td>
      <td>2</td>
      <td>55</td>
    </tr>
    <tr>
      <td>35</td>
      <td>1</td>
      <td>45</td>
    </tr>
    <tr>
      <td>37</td>
      <td>1</td>
      <td>45</td>
    </tr>
    <tr>
      <td>44</td>
      <td>2</td>
      <td>43</td>
    </tr>
    <tr>
      <td>55</td>
      <td>2</td>
      <td>43</td>
    </tr>
    <tr>
      <td>70</td>
      <td>2</td>
      <td>43</td>
    </tr>
    <tr>
      <td>76</td>
      <td>2</td>
      <td>43</td>
    </tr>
    <tr>
      <td>71</td>
      <td>2</td>
      <td>40</td>
    </tr>
    <tr>
      <td>40</td>
      <td>1</td>
      <td>36</td>
    </tr>
    <tr>
      <td>31</td>
      <td>1</td>
      <td>29</td>
    </tr>
    <tr>
      <td>74</td>
      <td>1</td>
      <td>29</td>
    </tr>
    <tr>
      <td>80</td>
      <td>1</td>
      <td>26</td>
    </tr>
    <tr>
      <td>49</td>
      <td>1</td>
      <td>14</td>
    </tr>
    <tr>
      <td>58</td>
      <td>1</td>
      <td>14</td>
    </tr>
    <tr>
      <td>73</td>
      <td>1</td>
      <td>14</td>
    </tr>
  </table>
  <h2>Leg load</h2>
  <p>(only legs used by more than one course appear in the following table)</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th># Courses</th>
      <th>Load</th>
    </tr>
    <tr>
      <td>38–Finish</td>
      <td>6</td>
      <td>227</td>
    </tr>
    <tr>
      <td>47–48</td>
      <td>3</td>
      <td>148</td>
    </tr>
    <tr>
      <td>57–79</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>48–50</td>
      <td>2</td>
      <td>81</td>
    </tr>
    <tr>
      <td>Start–59</td>
      <td>2</td>
      <td>71</td>
    </tr>
    <tr>
      <td>55–38</td>
      <td>2</td>
      <td>43</td>
    </tr>
  </table>

";

            Assert.AreEqual(expected, result);
        }


        [TestMethod]
        public void CourseLoad3()
        {
            Setup(@"reports\visitload.ppen");

            Reports reports = new Reports();
            string result = reports.CreateLoadReport(eventDB);

            Assert.AreEqual(@"
  <h1>Competitor Load Summary for variations</h1>
  <p>
    <strong>NOTE: </strong> One or more courses has variations. Load numbers will be computed assuming that competitors are evenly distributed between forks.</p>
  <p>
    <strong>NOTE: </strong> Some controls are visited multiple times on the same course. The second load number counts each visit to a control separately.</p>
  <h2>Control load</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Control</th>
      <th># Courses</th>
      <th>Load</th>
      <th>Visits</th>
    </tr>
    <tr>
      <td>34</td>
      <td>1</td>
      <td>180</td>
      <td>180</td>
    </tr>
    <tr>
      <td>49</td>
      <td>1</td>
      <td>180</td>
      <td>180</td>
    </tr>
    <tr>
      <td>33</td>
      <td>1</td>
      <td>100</td>
      <td>300</td>
    </tr>
    <tr>
      <td>40</td>
      <td>1</td>
      <td>100</td>
      <td>100</td>
    </tr>
    <tr>
      <td>42</td>
      <td>1</td>
      <td>100</td>
      <td>100</td>
    </tr>
    <tr>
      <td>45</td>
      <td>1</td>
      <td>100</td>
      <td>100</td>
    </tr>
    <tr>
      <td>46</td>
      <td>1</td>
      <td>100</td>
      <td>100</td>
    </tr>
    <tr>
      <td>47</td>
      <td>1</td>
      <td>100</td>
      <td>100</td>
    </tr>
    <tr>
      <td>48</td>
      <td>1</td>
      <td>100</td>
      <td>100</td>
    </tr>
    <tr>
      <td>36</td>
      <td>1</td>
      <td>90</td>
      <td>270</td>
    </tr>
    <tr>
      <td>37</td>
      <td>1</td>
      <td>90</td>
      <td>90</td>
    </tr>
    <tr>
      <td>38</td>
      <td>1</td>
      <td>90</td>
      <td>90</td>
    </tr>
    <tr>
      <td>39</td>
      <td>1</td>
      <td>90</td>
      <td>90</td>
    </tr>
    <tr>
      <td>50</td>
      <td>1</td>
      <td>90</td>
      <td>90</td>
    </tr>
    <tr>
      <td>51</td>
      <td>1</td>
      <td>90</td>
      <td>90</td>
    </tr>
    <tr>
      <td>31</td>
      <td>0</td>
      <td />
      <td />
    </tr>
    <tr>
      <td>32</td>
      <td>0</td>
      <td />
      <td />
    </tr>
    <tr>
      <td>35</td>
      <td>0</td>
      <td />
      <td />
    </tr>
    <tr>
      <td>41</td>
      <td>0</td>
      <td />
      <td />
    </tr>
    <tr>
      <td>43</td>
      <td>0</td>
      <td />
      <td />
    </tr>
    <tr>
      <td>52</td>
      <td>0</td>
      <td />
      <td />
    </tr>
  </table>
  <h2>Leg load</h2>
  <p>(only legs used by more than one course appear in the following table)</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th># Courses</th>
      <th>Load</th>
    </tr>
  </table>

", result);
        }


        [TestMethod]
        public void CrossRef()
        {
            Setup(@"reports\marymoor4.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateCrossReferenceReport(eventDB);

            Assert.AreEqual(@"
  <h1>Control cross-reference for Marymoor WIOL 2</h1>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Control</th>
      <th>Course 1</th>
      <th>Course 2</th>
      <th>Course 3</th>
      <th>Course 4B</th>
      <th>Course 4G</th>
      <th>Course 5</th>
      <th>Empty</th>
      <th>Long</th>
      <th>Score</th>
      <th>Relay</th>
    </tr>
    <tr>
      <td>31</td>
      <td />
      <td>7</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>4,5</td>
    </tr>
    <tr>
      <td>35</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>10</td>
      <td />
      <td />
      <td>*</td>
      <td>1</td>
    </tr>
    <tr>
      <td class=""tablerule"">36</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">9</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">12</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">7</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>37</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>11</td>
      <td />
      <td>8</td>
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td>38</td>
      <td>10</td>
      <td>11</td>
      <td>13</td>
      <td>13</td>
      <td>12</td>
      <td>18</td>
      <td />
      <td />
      <td />
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">39</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">7</td>
      <td class=""tablerule"">13</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">6</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>40</td>
      <td />
      <td />
      <td />
      <td>10</td>
      <td />
      <td />
      <td />
      <td>15</td>
      <td>*</td>
      <td>2</td>
    </tr>
    <tr>
      <td>41</td>
      <td />
      <td />
      <td>3</td>
      <td>3</td>
      <td>11</td>
      <td>16</td>
      <td />
      <td />
      <td>*</td>
      <td>7,8,10,11</td>
    </tr>
    <tr>
      <td class=""tablerule"">42</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">1</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">17</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>43</td>
      <td />
      <td />
      <td>9</td>
      <td />
      <td />
      <td>14</td>
      <td />
      <td>5</td>
      <td>*</td>
      <td>3</td>
    </tr>
    <tr>
      <td>44</td>
      <td>2</td>
      <td>2</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">45</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">11</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">5</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">10</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>46</td>
      <td />
      <td />
      <td />
      <td>4</td>
      <td />
      <td>3</td>
      <td />
      <td>2</td>
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td>47</td>
      <td />
      <td />
      <td>5</td>
      <td />
      <td>3</td>
      <td>4</td>
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">48</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">5</td>
      <td class=""tablerule"">4</td>
      <td class=""tablerule"">5</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">4,11,20</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>49</td>
      <td>3</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>1</td>
      <td>*</td>
      <td>13,14</td>
    </tr>
    <tr>
      <td>50</td>
      <td />
      <td>8</td>
      <td />
      <td>6</td>
      <td />
      <td>6</td>
      <td />
      <td>16</td>
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">51</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">2</td>
      <td class=""tablerule"">2</td>
      <td class=""tablerule"">2</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">12,17</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"">7,8,11,12</td>
    </tr>
    <tr>
      <td>52</td>
      <td />
      <td />
      <td />
      <td />
      <td>1</td>
      <td />
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td>53</td>
      <td>4</td>
      <td>3</td>
      <td>2</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td>8,9,11,12</td>
    </tr>
    <tr>
      <td class=""tablerule"">54</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">12</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">15</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>55</td>
      <td>9</td>
      <td>10</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>21</td>
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td>56</td>
      <td />
      <td />
      <td />
      <td />
      <td>6</td>
      <td>7</td>
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">57</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">7</td>
      <td class=""tablerule"">9</td>
      <td class=""tablerule"">8</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">*</td>
      <td class=""tablerule"">3</td>
    </tr>
    <tr>
      <td>58</td>
      <td>5</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td>59</td>
      <td />
      <td />
      <td>1</td>
      <td />
      <td />
      <td>1</td>
      <td />
      <td>18</td>
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">60</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">3</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>70</td>
      <td>7</td>
      <td>6</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>19</td>
      <td>*</td>
      <td>6,7,9,10</td>
    </tr>
    <tr>
      <td>71</td>
      <td>8</td>
      <td />
      <td>6</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">72</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">5</td>
      <td class=""tablerule"">4</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">14</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"">5,6,8,9,10,12,13</td>
    </tr>
    <tr>
      <td>73</td>
      <td>1</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
    </tr>
    <tr>
      <td>74</td>
      <td />
      <td>1</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td class=""tablerule"">75</td>
      <td class=""tablerule"" />
      <td class=""tablerule"">9</td>
      <td class=""tablerule"">10</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">*</td>
      <td class=""tablerule"">3</td>
    </tr>
    <tr>
      <td>76</td>
      <td>6</td>
      <td>4</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td>6,7,10,11</td>
    </tr>
    <tr>
      <td>77</td>
      <td />
      <td />
      <td>7</td>
      <td />
      <td>10</td>
      <td />
      <td />
      <td />
      <td>*</td>
      <td>4</td>
    </tr>
    <tr>
      <td class=""tablerule"">78</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">8</td>
      <td class=""tablerule"">11</td>
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"" />
      <td class=""tablerule"">9</td>
      <td class=""tablerule"">*</td>
      <td class=""tablerule"" />
    </tr>
    <tr>
      <td>79</td>
      <td />
      <td />
      <td />
      <td>8</td>
      <td>8</td>
      <td>9</td>
      <td />
      <td>13</td>
      <td>*</td>
      <td />
    </tr>
    <tr>
      <td>80</td>
      <td />
      <td />
      <td>12</td>
      <td />
      <td />
      <td />
      <td />
      <td />
      <td>*</td>
      <td />
    </tr>
  </table>

", result);
    }

        [TestMethod]
        public void NearbyControls1()
        {
            Setup(@"reports\close1.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            Assert.AreEqual(@"
  <h1>Event Audit for Sample Event</h1>
  <p>No problems found.</p>

", result);
        }

        [TestMethod]
        public void NearbyControls2()
        {
            Setup(@"reports\close2.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            Assert.AreEqual(@"
  <h1>Event Audit for Sample Event</h1>
  <h2>Close Together Controls</h2>
  <p>The following table shows all control pairs that are within 100 meters of each other. The same symbol column shows whether the two controls have the same primary symbol (column D).</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Control codes</th>
      <th>Distance</th>
      <th>Same symbol?</th>
    </tr>
    <tr>
      <td>58, 59</td>
      <td>69 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>39, 62</td>
      <td>91 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>52, 62</td>
      <td>92 m</td>
      <td>Yes</td>
    </tr>
    <tr>
      <td>46, 47</td>
      <td>93 m</td>
      <td>Yes</td>
    </tr>
  </table>
  <h2>Unused Controls</h2>
  <p>The following controls are present in the All Controls collection but are not used in any course. To remove them, use the ""Remove Unused Controls"" command on the ""Event"" menu. These controls will not be considered further in this report.</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Code</th>
      <th>Location</th>
    </tr>
    <tr>
      <td>58</td>
      <td>(49, 119)</td>
    </tr>
    <tr>
      <td>59</td>
      <td>(52, 116)</td>
    </tr>
    <tr>
      <td>60</td>
      <td>(-1, 63)</td>
    </tr>
    <tr>
      <td>62</td>
      <td>(-37, 98)</td>
    </tr>
    <tr>
      <td>Crossing</td>
      <td>(36, 164)</td>
    </tr>
    <tr>
      <td>Crossing</td>
      <td>(37, 164)</td>
    </tr>
  </table>

", result);
        }

        [TestMethod]
        public void EventAudit()
        {
            Setup(@"reports\marymoor6.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            Assert.AreEqual(@"
  <h1>Event Audit for Marymoor WIOL 2</h1>
  <h2>Missing Items in Courses</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Course</th>
      <th>Item</th>
      <th>Reason</th>
    </tr>
    <tr>
      <td>Course 1</td>
      <td>Climb</td>
      <td>Regular courses should indicate the amount of climb</td>
    </tr>
    <tr>
      <td>Course 2</td>
      <td>Finish</td>
      <td>Regular courses should have a finish circle</td>
    </tr>
    <tr>
      <td>Course 2</td>
      <td>Climb</td>
      <td>Regular courses should indicate the amount of climb</td>
    </tr>
    <tr>
      <td>Course 2</td>
      <td>Load</td>
      <td>Course should have expected competitor load</td>
    </tr>
    <tr>
      <td>Course 4B</td>
      <td>Climb</td>
      <td>Regular courses should indicate the amount of climb</td>
    </tr>
    <tr>
      <td>Course 4G</td>
      <td>Start</td>
      <td>Regular courses should have a start triangle</td>
    </tr>
    <tr>
      <td>Course 4G</td>
      <td>Climb</td>
      <td>Regular courses should indicate the amount of climb</td>
    </tr>
    <tr>
      <td>Score 2</td>
      <td>Start</td>
      <td>Score courses should have a start triangle</td>
    </tr>
    <tr>
      <td>Score 3</td>
      <td>Load</td>
      <td>Course should have expected competitor load</td>
    </tr>
  </table>
  <h2>Close Together Controls</h2>
  <p>The following table shows all control pairs that are within 100 meters of each other. The same symbol column shows whether the two controls have the same primary symbol (column D).</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Control codes</th>
      <th>Distance</th>
      <th>Same symbol?</th>
    </tr>
    <tr>
      <td>54, 55</td>
      <td>34 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>42, 52</td>
      <td>54 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>73, 74</td>
      <td>63 m</td>
      <td>Yes</td>
    </tr>
    <tr>
      <td>38, 54</td>
      <td>63 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>50, 71</td>
      <td>74 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>38, 55</td>
      <td>77 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>56, 77</td>
      <td>82 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>75, 77</td>
      <td>83 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>53, 58</td>
      <td>85 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>46, 72</td>
      <td>87 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>51, 58</td>
      <td>88 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>42, 44</td>
      <td>88 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>36, 79</td>
      <td>90 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>38, 80</td>
      <td>93 m</td>
      <td>Yes</td>
    </tr>
    <tr>
      <td>51, 53</td>
      <td>95 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>39, 40</td>
      <td>96 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>57, 78</td>
      <td>96 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>56, 75</td>
      <td>98 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>56, 57</td>
      <td>99 m</td>
      <td>No</td>
    </tr>
    <tr>
      <td>55, 80</td>
      <td>100 m</td>
      <td>No</td>
    </tr>
  </table>
  <h2>Unused Controls</h2>
  <p>The following controls are present in the All Controls collection but are not used in any course. To remove them, use the ""Remove Unused Controls"" command on the ""Event"" menu. These controls will not be considered further in this report.</p>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Code</th>
      <th>Location</th>
    </tr>
    <tr>
      <td>Start</td>
      <td>(30, 40)</td>
    </tr>
    <tr>
      <td>31</td>
      <td>(-10, -11)</td>
    </tr>
    <tr>
      <td>32</td>
      <td>(84, -16)</td>
    </tr>
    <tr>
      <td>60</td>
      <td>(-13, 25)</td>
    </tr>
    <tr>
      <td>Finish</td>
      <td>(-2, -31)</td>
    </tr>
    <tr>
      <td>Crossing</td>
      <td>(-23, -3)</td>
    </tr>
  </table>
  <h2>Missing Description Boxes</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Code</th>
      <th>Column</th>
      <th>Reason</th>
    </tr>
    <tr>
      <td>48</td>
      <td>D</td>
      <td>All controls must have a main feature in column D</td>
    </tr>
    <tr>
      <td>49</td>
      <td>E</td>
      <td>When ""junction"" or ""crossing"" is in column F, two features must be shown in columns D and E</td>
    </tr>
    <tr>
      <td>52</td>
      <td>D</td>
      <td>All controls must have a main feature in column D</td>
    </tr>
    <tr>
      <td>57</td>
      <td>E</td>
      <td>When ""between"" is in column G, two features must be shown in columns D and E</td>
    </tr>
    <tr>
      <td>78</td>
      <td>D</td>
      <td>All controls must have a main feature in column D</td>
    </tr>
  </table>
  <h2>Missing Punch Patterns</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Code</th>
      <th>Reason</th>
    </tr>
    <tr>
      <td>42</td>
      <td>No punch pattern defined</td>
    </tr>
    <tr>
      <td>45</td>
      <td>No punch pattern defined</td>
    </tr>
    <tr>
      <td>55</td>
      <td>No punch pattern defined</td>
    </tr>
    <tr>
      <td>70</td>
      <td>No punch pattern defined</td>
    </tr>
  </table>
  <h2>Missing Scores</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol leftalign"" />
    <tr>
      <th>Course</th>
      <th>Control</th>
      <th>Reason</th>
    </tr>
    <tr>
      <td>Score 1</td>
      <td>80</td>
      <td>Score course should have score set for all controls or no controls</td>
    </tr>
    <tr>
      <td>Score 1</td>
      <td>70</td>
      <td>Score course should have score set for all controls or no controls</td>
    </tr>
  </table>

", result);
        }

        [TestMethod]
        public void CourseSummary()
        {
            Setup(@"reports\marymoor.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateCourseSummaryReport(eventDB);

            Assert.AreEqual(@"
  <h1>Course Summary for Marymoor WIOL 2</h1>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Course</th>
      <th>Controls</th>
      <th>Length</th>
      <th>Climb</th>
    </tr>
    <tr>
      <td>Course 1</td>
      <td>10</td>
      <td>1.5 km</td>
      <td>5 m</td>
    </tr>
    <tr>
      <td>Course 2</td>
      <td>11</td>
      <td>2.1 km</td>
      <td>20 m</td>
    </tr>
    <tr>
      <td>Course 3</td>
      <td>13</td>
      <td>2.9 km</td>
      <td>25 m</td>
    </tr>
    <tr>
      <td>Course 4B</td>
      <td>13</td>
      <td>3.7 km</td>
      <td />
    </tr>
    <tr>
      <td>Course 4G</td>
      <td>12</td>
      <td>3.7 km</td>
      <td>80 m</td>
    </tr>
    <tr>
      <td>Course 5</td>
      <td>18</td>
      <td>5.0 km</td>
      <td>105 m</td>
    </tr>
    <tr>
      <td>Empty</td>
      <td>0</td>
      <td>0.0 km</td>
      <td />
    </tr>
    <tr>
      <td>Long</td>
      <td>21</td>
      <td>11.9 km</td>
      <td>190 m</td>
    </tr>
    <tr>
      <td>Score</td>
      <td>34</td>
      <td />
      <td />
    </tr>
  </table>

", result);

        }

        [TestMethod]
        public void CourseSummary2()
        {
            Setup(@"reports\relay1.ppen");

            Reports reports = new Reports();
            string result = reports.CreateCourseSummaryReport(eventDB);

            Assert.AreEqual(@"
  <h1>Course Summary for Test Event</h1>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""middlecol rightalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Course</th>
      <th>Controls</th>
      <th>Length</th>
      <th>Climb</th>
    </tr>
    <tr>
      <td>Relay</td>
      <td>5–7</td>
      <td>3.3–4.1 km</td>
      <td />
    </tr>
    <tr>
      <td>    AD</td>
      <td>6</td>
      <td>3.7 km</td>
      <td />
    </tr>
    <tr>
      <td>    AE</td>
      <td>6</td>
      <td>3.5 km</td>
      <td />
    </tr>
    <tr>
      <td>    AF</td>
      <td>6</td>
      <td>4.1 km</td>
      <td />
    </tr>
    <tr>
      <td>    BD</td>
      <td>7</td>
      <td>3.8 km</td>
      <td />
    </tr>
    <tr>
      <td>    BE</td>
      <td>7</td>
      <td>3.6 km</td>
      <td />
    </tr>
    <tr>
      <td>    BF</td>
      <td>7</td>
      <td>4.1 km</td>
      <td />
    </tr>
    <tr>
      <td>    CD</td>
      <td>5</td>
      <td>3.5 km</td>
      <td />
    </tr>
    <tr>
      <td>    CE</td>
      <td>5</td>
      <td>3.3 km</td>
      <td />
    </tr>
    <tr>
      <td>    CF</td>
      <td>5</td>
      <td>3.9 km</td>
      <td />
    </tr>
    <tr>
      <td>Normal</td>
      <td>2</td>
      <td>2.2 km</td>
      <td />
    </tr>
    <tr>
      <td>Score</td>
      <td>4</td>
      <td />
      <td />
    </tr>
  </table>

", result);

        }

        [TestMethod]
        public void LegLength()
        {
            Setup(@"reports\marymoor5.ppen");

            Reports reports = new Reports();
            string result = reports.CreateLegLengthReport(eventDB);

            string expected = @"
  <h1>Leg Length Report for Marymoor WIOL 2</h1>
  <h2>Course 1 (10 controls, 1.6 km)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
    <tr>
      <td>1</td>
      <td>Start–73</td>
      <td>51 m</td>
    </tr>
    <tr>
      <td>2</td>
      <td>73–44</td>
      <td>344 m</td>
    </tr>
    <tr>
      <td>3</td>
      <td>44–49</td>
      <td>157 m</td>
    </tr>
    <tr>
      <td>4</td>
      <td>49–53</td>
      <td>110 m</td>
    </tr>
    <tr>
      <td>5</td>
      <td>53–58</td>
      <td>85 m</td>
    </tr>
    <tr>
      <td>6</td>
      <td>58–76</td>
      <td>148 m</td>
    </tr>
    <tr>
      <td>7</td>
      <td>76–70</td>
      <td>235 m</td>
    </tr>
    <tr>
      <td>8</td>
      <td>70–71</td>
      <td>196 m</td>
    </tr>
    <tr>
      <td>9</td>
      <td>71–55</td>
      <td>131 m</td>
    </tr>
    <tr>
      <td>10</td>
      <td>55–38</td>
      <td>77 m</td>
    </tr>
    <tr>
      <td>11</td>
      <td>38–Finish</td>
      <td>64 m</td>
    </tr>
    <tr class=""summaryrow"">
      <td colspan=""2"">Average</td>
      <td>145 m</td>
    </tr>
  </table>
  <h2>Course 2 (3 controls, 2.2 km, 155 m climb)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
    <tr>
      <td>1</td>
      <td>Start–59</td>
      <td>630 m</td>
    </tr>
    <tr>
      <td>2</td>
      <td>59–72</td>
      <td>787 m</td>
    </tr>
    <tr>
      <td>3</td>
      <td>72–48</td>
      <td>372 m</td>
    </tr>
    <tr>
      <td>4</td>
      <td>48–Finish</td>
      <td>383 m</td>
    </tr>
    <tr class=""summaryrow"">
      <td colspan=""2"">Average</td>
      <td>543 m</td>
    </tr>
  </table>
  <h2>Empty (0 controls, 0.0 km, 15 m climb)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
  </table>
  <h2>NoFinish (2 controls, 1.0 km)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
    <tr>
      <td>1</td>
      <td>Start–58</td>
      <td>661 m</td>
    </tr>
    <tr>
      <td>2</td>
      <td>58–50</td>
      <td>366 m</td>
    </tr>
    <tr class=""summaryrow"">
      <td colspan=""2"">Average</td>
      <td>514 m</td>
    </tr>
  </table>
  <h2>NoStart (2 controls, 0.4 km, 35 m climb)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
    <tr>
      <td>1</td>
      <td>80–50</td>
      <td>163 m</td>
    </tr>
    <tr>
      <td>2</td>
      <td>50–Finish</td>
      <td>245 m</td>
    </tr>
    <tr class=""summaryrow"">
      <td colspan=""2"">Average</td>
      <td>204 m</td>
    </tr>
  </table>

";
            if (expected != result) {
                TestUtil.WriteStringDifference(expected, result);
            }

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void LegLength2()
        {
            Setup(@"reports\relay2.ppen");

            Reports reports = new Reports();
            string result = reports.CreateLegLengthReport(eventDB);

            string expected = @"
  <h1>Leg Length Report for rgerg</h1>
  <h2>Relay (5–7 controls, 3.2–3.8 km)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
    <tr>
      <td />
      <td>Start–31</td>
      <td>974 m</td>
    </tr>
    <tr>
      <td />
      <td>31–34</td>
      <td>319 m</td>
    </tr>
    <tr>
      <td />
      <td>31–35</td>
      <td>206 m</td>
    </tr>
    <tr>
      <td />
      <td>31–32</td>
      <td>532 m</td>
    </tr>
    <tr>
      <td />
      <td>35–36</td>
      <td>209 m</td>
    </tr>
    <tr>
      <td />
      <td>36–32</td>
      <td>381 m</td>
    </tr>
    <tr>
      <td />
      <td>34–32</td>
      <td>396 m</td>
    </tr>
    <tr>
      <td />
      <td>32–33</td>
      <td>412 m</td>
    </tr>
    <tr>
      <td />
      <td>33–38</td>
      <td>395 m</td>
    </tr>
    <tr>
      <td />
      <td>33–39</td>
      <td>339 m</td>
    </tr>
    <tr>
      <td />
      <td>33–40</td>
      <td>627 m</td>
    </tr>
    <tr>
      <td />
      <td>39–37</td>
      <td>359 m</td>
    </tr>
    <tr>
      <td />
      <td>40–37</td>
      <td>423 m</td>
    </tr>
    <tr>
      <td />
      <td>38–37</td>
      <td>613 m</td>
    </tr>
    <tr>
      <td />
      <td>37–Finish</td>
      <td>595 m</td>
    </tr>
    <tr class=""summaryrow"">
      <td colspan=""2"">Average</td>
      <td>452 m</td>
    </tr>
  </table>
  <h2>RelayCross (5–7 controls, 3.3–4.2 km)</h2>
  <table>
    <col class=""leftcol leftalign"" />
    <col class=""middlecol leftalign"" />
    <col class=""rightcol rightalign"" />
    <tr>
      <th>Leg</th>
      <th>Controls</th>
      <th>Length</th>
    </tr>
    <tr>
      <td />
      <td>Start–31</td>
      <td>992 m</td>
    </tr>
    <tr>
      <td />
      <td>31–34</td>
      <td>319 m</td>
    </tr>
    <tr>
      <td />
      <td>31–35</td>
      <td>206 m</td>
    </tr>
    <tr>
      <td />
      <td>31–32</td>
      <td>533 m</td>
    </tr>
    <tr>
      <td />
      <td>35–36</td>
      <td>209 m</td>
    </tr>
    <tr>
      <td />
      <td>36–32</td>
      <td>381 m</td>
    </tr>
    <tr>
      <td />
      <td>34–32</td>
      <td>396 m</td>
    </tr>
    <tr>
      <td />
      <td>32–33</td>
      <td>412 m</td>
    </tr>
    <tr>
      <td />
      <td>33–38</td>
      <td>395 m</td>
    </tr>
    <tr>
      <td />
      <td>33–39</td>
      <td>339 m</td>
    </tr>
    <tr>
      <td />
      <td>33–40</td>
      <td>627 m</td>
    </tr>
    <tr>
      <td />
      <td>39–37</td>
      <td>450 m</td>
    </tr>
    <tr>
      <td />
      <td>40–37</td>
      <td>734 m</td>
    </tr>
    <tr>
      <td />
      <td>38–37</td>
      <td>620 m</td>
    </tr>
    <tr>
      <td />
      <td>37–Finish</td>
      <td>595 m</td>
    </tr>
    <tr class=""summaryrow"">
      <td colspan=""2"">Average</td>
      <td>480 m</td>
    </tr>
  </table>

";
            if (expected != result) {
                TestUtil.WriteStringDifference(expected, result);
            }

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestReport()
        {
            Setup(@"reports\marymoor.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateTestReport(eventDB);
            Assert.AreEqual(@"
  <h1>Test Report</h1>
  <h2>Heading &amp; cool stuph 2</h2>
  <p>The first paragraph: x+3 &lt; 4</p>
  <p class=""coolclass"">The second paragraph</p>
  <p class=""coolclass"">This is the start of paragraph, with <strong>bold</strong> text and <em>italic</em> text and <u>underline</u> text and <strike>strikeout</strike> text and <strong><u>combo</u></strong> text. </p>
  <p class=""paraclass"">This is a paragraph with style paraclass</p>
  <table>
    <col class=""leftcol"" />
    <col class=""middlecol"" />
    <col class=""rightcol"" />
    <tr>
      <th>Column 1</th>
      <th>Column 2</th>
      <th>Column 3</th>
    </tr>
    <tr>
      <td>row1col1</td>
      <td>row1col2</td>
      <td>row1col3</td>
    </tr>
    <tr>
      <td />
      <td>row2col2</td>
      <td>row2col3</td>
    </tr>
    <tr>
      <td>row3col1</td>
      <td>row3col2</td>
      <td>row3col3</td>
    </tr>
    <tr>
      <td colspan=""2"">row3col1and2</td>
      <td>row3col3</td>
    </tr>
  </table>
  <table class=""tableClass"">
    <col class=""leftcol col1Class"" />
    <col class=""middlecol col2Class"" />
    <col class=""middlecol"" />
    <col class=""rightcol"" />
    <tr>
      <th />
      <th class=""myklass"">row1col2</th>
      <th>row1col3</th>
      <th />
      <th class=""anotherclass"">row1col5</th>
    </tr>
    <tr>
      <td>row2col1</td>
      <td class=""myklass"">row2col2</td>
      <td>row2col3</td>
      <td />
      <td>row2col5</td>
    </tr>
  </table>

", result);
        }
    }
}

#endif //TEST

$(function ()
{
    Office.initialize = function (reason)
    {
        $(document).ready(function () {
            BigNumber.config({ EXPONENTIAL_AT: 1e+9 });
            let token = sessionStorage["token"];

            if (!token)
                window.location.replace("/");

            point.init(token);
            $("#dvLogin").hide();
            $("#powerpoint-addin").show();
        });

    };
});

var point = (function () {
    var point = {
        token: "",
        filePath: "",
        controls: {},
        selectDocument: null,
        file: null,
        points: [],
        sourcePointKeyword: "",
        pagerIndex: 0,
        pagerSize: 30,
        pagerCount: 0,
        totalPoints: 0,
        endpoints: {
            catalog: "/api/SourcePointCatalog?documentId=",
            token: "/api/GraphAccessToken",
            sharePointToken: "/api/SharePointAccessToken",
            graph: "https://graph.microsoft.com/v1.0",
            userInfo: "/api/userprofile",
            customFormat: "/api/CustomFormats"
        },
        api: {
            host: "",
            token: "",
            sharePointToken: ""
        }
    }, that = point;

    that.init = function (accessToken) {
        that.token = accessToken;
        that.filePath = Office.context.document.url;
        that.controls = {
            body: $("body"),
            main: $(".main"),
            select: $(".n-select"),
            refresh: $(".n-refresh"),
            titleName: $("#lblSourcePointName"),
            sourcePointName: $("#txtSearchSourcePoint"),
            searchSourcePoint: $("#iSearchSourcePoint"),
            autoCompleteControl2: $("#autoCompleteWrap2"),
            list: $("#listPoints"),
            headerListPoints: $("#headerListPoints"),
            popupMain: $("#popupMain"),
            popupErrorOK: $("#btnErrorOK"),
            popupMessage: $("#popupMessage"),
            popupProcessing: $("#popupProcessing"),
            popupSuccessMessage: $("#lblSuccessMessage"),
            popupErrorMain: $("#popupErrorMain"),
            popupErrorTitle: $("#lblErrorTitle"),
            popupErrorMessage: $("#lblErrorMessage"),
            popupErrorRepair: $("#lblErrorRepair"),
            popupBrowseList: $("#browseList"),
            popupBrowseBack: $("#btnBrowseBack"),
            popupBrowseCancel: $("#btnBrowseCancel"),
            popupBrowseMessage: $("#txtBrowseMessage"),
            popupBrowseLoading: $("#popBrowseLoading"),
            /*Message Bar begin*/
            innerMessageBox: $("#innerMessageBox"),
            innerMessageIcon: $("#innerMessageIcon"),
            innerMessageText: $("#innerMessageText"),
            /*Message Bar end*/
            pager: $("#pager"),
            pagerTotal: $("#pagerTotal"),
            pagerPages: $("#pagerPages"),
            pagerCurrent: $("#pagerCurrent"),
            pagerPrev: $("#pagerPrev"),
            pagerNext: $("#pagerNext"),
            pagerValue: $("#pagerValue"),
            pagerGo: $("#pagerGo"),
            indexes: $("#indexes"),
            /* Footer begin */
            footer: $(".footer"),
            settings: $(".settings"),
            openSettings: $(".f-settings"),
            closeSettings: $(".s-settings"),
            userName: $(".s-username"),
            email: $(".s-email"),
            // Navigation
            sourceTypeNavMana: $(".point-types-mana li"),
            sourceTypeNav: $(".point-types li"),
            headerListPointsAdd: $("#headerListPointsAdd")
        };

        that.controls.body.click(function () {
            that.action.body();
        });
        that.controls.select.click(function () {
            that.browse.init();
        });
        that.controls.refresh.click(function () {
            if (that.selectDocument != null) {
                that.list();
            }
            else {
                that.popup.message({ success: false, title: "Please select file first." });
            }
        });
        that.controls.sourcePointName.focus(function () {
            that.action.dft(this, true);
        });
        that.controls.sourcePointName.blur(function () {
            that.action.dft(this, false);
        });
        that.controls.sourcePointName.keydown(function (e) {
            if (e.keyCode == 13) {
                if (that.controls.sourcePointName.val() != "") {
                    $(".search-tooltips").hide();
                    that.action.searchSourcePoint(true);
                }
            }
            else if (e.keyCode == 38 || e.keyCode == 40) {
                app.search.move({ result: that.controls.autoCompleteControl2, target: that.controls.sourcePointName, down: e.keyCode == 40 });
            }
        });
        that.controls.sourcePointName.bind("input", function (e) {
            that.action.autoComplete2();
        });
        that.controls.searchSourcePoint.click(function () {
            that.action.searchSourcePoint(!that.controls.sourcePointName.closest(".search").hasClass("searched"));
        });
        that.controls.popupErrorOK.click(function () {
            that.action.ok();
        });
        that.controls.popupBrowseList.on("click", "li", function () {
            that.browse.select($(this));
        });
        that.controls.popupBrowseCancel.click(function () {
            that.browse.popup.hide();
        });
        that.controls.popupBrowseBack.click(function () {
            that.browse.popup.back();
        });

        that.controls.innerMessageBox.on("click", ".close-Message", function () {
            that.popup.hide();
            return false;
        });

        that.controls.list.on("click", ".i-history", function () {
            that.action.history($(this).closest(".point-item"));
            return false;
        });

        that.controls.list.on("click", "li .i-add", function () {
            $(this).blur();
            that.action.save($(this));
            return false;
        });

        that.controls.list.on("click", ".point-item", function () {
            that.action.addSourcePoint($(this));
            return false;
        });

        that.controls.list.on("click", "li .btnSelectFormat", function () {
            $(this).closest(".point-item").find(".listFormats").hasClass("active") ? $(this).closest(".point-item").find(".listFormats").removeClass("active") : $(this).closest(".point-item").find(".listFormats").addClass("active");
            return false;
        });
        that.controls.list.on("click", "li .iconSelectFormat", function () {
            $(this).closest(".point-item").find(".listFormats").hasClass("active") ? $(this).closest(".point-item").find(".listFormats").removeClass("active") : $(this).closest(".point-item").find(".listFormats").addClass("active");
            return false;
        });
        that.controls.list.on("click", "li .listFormats ul > li", function () {
            var _ck = $(this).hasClass("checked"), _sg = $(this).closest(".drp-radio").length > 0, _cn = $(this).data("name");
            if (_sg) {
                $(this).closest("ul").find("li").removeClass("checked");
            }
            _ck ? $(this).removeClass("checked") : $(this).addClass("checked");
            if (_cn == "ConvertToThousands" || _cn == "ConvertToMillions" || _cn == "ConvertToBillions" || _cn == "ConvertToHundreds") {
                $(this).closest(".listFormats").removeClass("convert1 convert2 convert3 convert4");
                $(this).closest(".listFormats").find(".drp-descriptor li.checked").removeClass("checked");
                if (!_ck) {
                    var _tn = _cn == "ConvertToThousands" ? "IncludeThousandDescriptor" : (_cn == "ConvertToMillions" ? "IncludeMillionDescriptor" : (_cn == "ConvertToBillions" ? "IncludeBillionDescriptor" : (_cn == "ConvertToHundreds" ? "IncludeHundredDescriptor" : "")));
                    var _cl = _cn == "ConvertToThousands" ? "convert2" : (_cn == "ConvertToMillions" ? "convert3" : (_cn == "ConvertToBillions" ? "convert4" : (_cn == "ConvertToHundreds" ? "convert1" : "")));
                    $(this).closest(".listFormats").addClass(_cl);
                }
            }
            that.action.selectedFormats($(this));
            return false;
        });

        that.controls.main.on("click", ".search-tooltips li", function () {
            $(this).parent().parent().find("input").val($(this).text());
            $(this).parent().hide();
            that.action.searchSourcePoint(true);
        });
        that.controls.main.on("mouseover", ".search-tooltips li", function () {
            $(this).parent().find("li.active").removeClass("active");
            $(this).addClass("active");
        });
        that.controls.main.on("mouseout", ".search-tooltips li", function () {
            $(this).removeClass("active");
        });
        that.controls.pagerPrev.on("click", function () {
            if (!$(this).hasClass("disabled")) {
                that.utility.pager.prev();
            }
        });
        that.controls.pagerNext.on("click", function () {
            if (!$(this).hasClass("disabled")) {
                that.utility.pager.next();
            }
        });
        that.controls.pagerValue.on("keydown", function (e) {
            if (e.keyCode == 13) {
                that.controls.pagerGo.click();
            }
        });
        that.controls.pagerGo.on("click", function () {
            var _v = $.trim(that.controls.pagerValue.val());
            if (isNaN(_v)) {
                that.popup.message({ success: false, title: "Only numbers are a valid input." });
            }
            else {
                _n = parseInt(_v);
                if (_n > 0 && _n <= that.pagerCount) {
                    that.utility.pager.init({ index: _n });
                }
                else {
                    that.popup.message({ success: false, title: "Invalid number." });
                }
            }
        });

        /* Footer begin */
        that.controls.settings.blur(function () {
            if (that.controls.footer.hasClass("footer-shorter")) {
                that.controls.footer.removeClass("footer-shorter");
                that.controls.settings.removeClass("show-settings");
            }
        });
        that.controls.openSettings.click(function () {
            that.controls.footer.addClass("footer-shorter");
            that.controls.settings.addClass("show-settings");
        });
        that.controls.closeSettings.click(function () {
            that.controls.footer.removeClass("footer-shorter");
            that.controls.settings.removeClass("show-settings");
        });
        /* Footer end */

        //Navegation
        that.controls.sourceTypeNavMana.click(function () {
            var _t = false;
            if ($(this).hasClass("is-selected")) {
                $(this).removeClass("is-selected");
                _t = true;
            } else {
                that.controls.sourceTypeNavMana.removeClass("is-selected");
                $(this).addClass("is-selected");
            }

            if ($(this).data("content") !== "Points" && !_t) {
                that.controls.headerListPoints.find(".i3 span")[0].innerText = window.stringResources["PublishedStatus"];
            }
            else {
                that.controls.headerListPoints.find(".i3 span")[0].innerText = window.stringResources["Value"];
            }
            $(".ckb-wrapper.all").find("input").prop("checked", false);
            $(".ckb-wrapper.all").removeClass("checked");
            that.utility.scrollTop();
            that.controls.list.find(".point-item").remove();
            //that.controls.moveUp.removeClass("disabled").addClass("disabled");
            //that.controls.moveDown.removeClass("disabled").addClass("disabled");
            that.utility.pager.init({ refresh: false });
        });
        that.controls.sourceTypeNav.click(function () {
            // avoid executing same item
            if (that.controls.sourceTypeNav.closest("li.ms-Pivot-link.is-selected").data("content") === $(this).data("content")) {
                return;
            }

            if ($(this).hasClass("is-selected")) {
                $(this).removeClass("is-selected");
            } else {
                that.controls.sourceTypeNav.removeClass("is-selected");
                $(this).addClass("is-selected");
            }

            if ($(this).data("content") !== "Points") {
                that.controls.headerListPointsAdd.find(".i3").hide();
            }
            else {
                that.controls.headerListPointsAdd.find(".i3").show();
            }

            //that.ui.sources({ data: that.file, keyword: that.keyword, sourceType: that.utility.selectedSourceType(".point-types") });
            that.ui.list();
        });

        $(window).resize(function () {
            that.utility.height();
        });
        that.utility.height();
        that.action.dft(that.controls.sourcePointName, false);
        that.utility.pager.status({ length: 0 });
        
        // Get user Info
        that.userInfo(function (result) {
            if (result.status === app.status.succeeded) {
                that.controls.userName[0].innerText = result.data.Username;
                that.controls.email[0].innerText = result.data.Email;
                that.popup.processing(false);
            }
            else {
                that.popup.message({ success: false, title: result.error.statusText });
            }
        });
    };

    that.userInfo = function (callback) {
        that.service.userInfo(function (result) {
            callback(result);
        });
    };

    that.list = function () {
        that.popup.processing(true);
        
        that.service.catalog({ documentId: that.selectDocument.Id }, function (result) {
            if (result.status === app.status.succeeded) {
                that.popup.processing(false);
                that.controls.titleName.html("Source Points in " + that.selectDocument.Name);
                that.controls.titleName.prop("title", "Source Points in " + that.selectDocument.Name);
                that.points = result.data && result.data.SourcePoints && result.data.SourcePoints.length > 0 ? result.data.SourcePoints : [];
                that.file = { SourcePoints: that.points };
                if (that.points.length > 0) {
                    that.utility.pager.init({ index: 1 });
                }
                else {
                    that.utility.pager.status({ length: 0 });
                }
            }
            else {
                that.popup.message({ success: false, title: "Load source points failed." });
            }
        });
    };

    that.utility = {
        selectedSourceType: function (sourceTypeClass) {
            var _i = $(sourceTypeClass).find("li.is-selected").index();
            if (_i === 0) {
                return app.sourceTypes.point;
            }
            else if (_i === 1) {
                return app.sourceTypes.chart;
            }
            else if (_i === 2) {
                return app.sourceTypes.table;
            }
            else {
                return app.sourceTypes.all;
            }
        },
        format: function (n) {
            return n > 9 ? n : ("0" + n);
        },
        date: function (str) {
            var _v = new Date(str), _d = _v.getDate(), _m = _v.getMonth() + 1, _y = _v.getFullYear(), _h = _v.getHours(), _mm = _v.getMinutes(), _a = _h < 12 ? " AM" : " PM";
            return that.utility.format(_m) + "/" + that.utility.format(_d) + "/" + _y + " " + (_h < 12 ? (_h == 0 ? "12" : that.utility.format(_h)) : (_h == 12 ? _h : _h - 12)) + ":" + that.utility.format(_mm) + "" + _a + " PST";
        },
        position: function (p) {
            if (p != null && p != undefined) {
                var _i = p.lastIndexOf("!"), _s = p.substr(0, _i).replace(new RegExp(/('')/g), '\''), _c = p.substr(_i + 1, p.length);
                if (_s.indexOf("'") == 0) {
                    _s = _s.substr(1, _s.length);
                }
                if (_s.lastIndexOf("'") == _s.length - 1) {
                    _s = _s.substr(0, _s.length - 1);
                }
                return { sheet: _s, cell: _c };
            }
            else {
                return { sheet: "", cell: "" };
            }
        },
        add: function (options) {
            that.points.push(options);
        },
        fileName: function (path) {
            path = decodeURI(path);
            return path.lastIndexOf("/") > -1 ? path.substr(path.lastIndexOf("/") + 1) : (path.lastIndexOf("\\") > -1 ? path.substr(path.lastIndexOf("\\") + 1) : path);
        },
        pager: {
            init: function (options) {
                that.controls.pagerValue.val("");
                that.controls.indexes.html("");
                that.pagerIndex = options.index ? options.index : 1;
                that.ui.list();
            },
            prev: function () {
                that.controls.pagerValue.val("");
                that.utility.pager.updatePager();
                that.pagerIndex--;
                that.ui.list();
            },
            next: function () {
                that.controls.pagerValue.val("");
                that.utility.pager.updatePager();
                that.pagerIndex++;
                that.ui.list();
            },
            status: function (options) {
                that.totalPoints = options.length;
                that.pagerCount = Math.ceil(options.length / that.pagerSize);
                that.controls.pagerTotal.html(options.length);
                that.controls.pagerPages.html(that.pagerCount);
                that.controls.pagerCurrent.html(that.pagerIndex);
                that.utility.pager.updatePager();
                that.pagerIndex == 1 || that.pagerCount == 0 ? that.controls.pagerPrev.addClass("disabled") : that.controls.pagerPrev.removeClass("disabled");
                that.pagerIndex == that.pagerCount || that.pagerCount == 0 ? that.controls.pagerNext.addClass("disabled") : that.controls.pagerNext.removeClass("disabled");
                if (that.totalPoints <= that.pagerSize) {
                    that.controls.pagerPrev.removeClass("disabled").addClass("disabled");
                    that.controls.pagerNext.removeClass("disabled").addClass("disabled");
                }
                else {
                    if (that.pagerIndex == 1) {
                        that.controls.pagerPrev.removeClass("disabled").addClass("disabled");
                    }
                    if (that.pagerIndex == that.pagerCount) {
                        that.controls.pagerNext.removeClass("disabled").addClass("disabled");
                    }
                }
            },
            updatePager: function () {
                var _start = ((that.pagerIndex - 1) * that.pagerSize + 1);
                var _left = that.totalPoints - _start;
                var _end = _left < 0 ? 0 : (_left <= that.pagerSize ? (_start + _left) : (_start + that.pagerSize - 1));
                if (_end > 0) {
                    that.controls.indexes.html(_start + "-" + _end);
                }
                else {
                    that.controls.indexes.html("");
                }
            }
        },
        height: function () {
            /*if (that.controls.main.hasClass("manage")) {
                var _h = that.controls.main.outerHeight();
                var _h1 = $("#pager").outerHeight();
                that.controls.list.css("maxHeight", (_h - 206 - 70 - _h1) + "px");
            }*/
        },
        path: function () {
            var _a = that.filePath.split("//")[1], _b = _a.split("/"), _p = [_b[0]];
            _b.shift();
            _b.pop();
            for (var i = 1; i <= _b.length; i++) {
                var _c = [];
                for (var n = 0; n < i; n++) {
                    _c.push(_b[n]);
                }
                _p.push(_c.join("/"));
            }
            return _p;
        },
        publishHistory: function (options) {
            var _ph = [], _tmd = options.data && options.data.length > 0 ? $.extend([], options.data) : [], _td = _tmd.reverse();
            $.each(_td, function (m, n) {
                var __c = _td[m].Value ? _td[m].Value : "",
                    __p = _td[m > 0 ? m - 1 : m].Value ? _td[m > 0 ? m - 1 : m].Value : "";
                if (m == 0 || __c != __p) {
                    _ph.push(n);
                }
            });
            return _ph.reverse().slice(0, 5);
        },
        selectedSource: function (id) {
            var _m = null;
            if (id && that.file.SourcePoints) {
                var _t = [];
                $.each(that.file.SourcePoints, function (i, d) {
                    _t.push(d.Id);
                });
                if ($.inArray(id, _t) > -1) {
                    _m = that.file.SourcePoints[$.inArray(id, _t)];
                }
            }
            return _m;
        }, 
        // Navegation
        scrollTop: function () {
            that.controls.list.scrollTop(0);
        },
    };

    that.action = {
        body: function () {
            $(".search-tooltips").hide();
        },
        dft: function (elem, on) {
            var _k = $.trim($(elem).val()), _kd = $(elem).data("default");
            if (on) {
                if (_k == _kd) {
                    $(elem).val("");
                }
                $(elem).removeClass("input-default");
            }
            else {
                if (_k == "" || _k == _kd) {
                    $(elem).val(_kd).addClass("input-default");
                }
            }
        },
        history: function (o) {
            o.hasClass("item-more") ? o.removeClass("item-more") : o.addClass("item-more");
        },
        ok: function () {
            that.controls.popupMain.removeClass("active message process confirm");
        },
        autoComplete2: function () {
            var _e = $.trim(that.controls.sourcePointName.val()), _d = that.points;
            if (_e != "") {
                app.search.autoComplete({ keyword: _e, data: _d, result: that.controls.autoCompleteControl2, target: that.controls.sourcePointName });
            }
            else {
                that.controls.autoCompleteControl2.hide();
            }
        },
        searchSourcePoint: function (s) {
            that.sourcePointKeyword = $.trim(that.controls.sourcePointName.val()) == that.controls.sourcePointName.data("default") ? "" : $.trim(that.controls.sourcePointName.val());
            if (s && that.sourcePointKeyword != "") {
                that.controls.sourcePointName.closest(".search").addClass("searched");
                that.controls.searchSourcePoint.removeClass("ms-Icon--Search").addClass("ms-Icon--Cancel");
            }
            else {
                that.sourcePointKeyword = "";
                that.controls.sourcePointName.val("");
                that.action.dft(that.controls.sourcePointName, false);
                that.controls.sourcePointName.closest(".search").removeClass("searched");
                that.controls.searchSourcePoint.removeClass("ms-Icon--Cancel").addClass("ms-Icon--Search");
            }
            that.utility.pager.init({});
        },
        addSourcePoint: function (o) {
            var _selectedItem = o.hasClass("point-item") ? o : o.closest(".point-item");
            var _i = _selectedItem.data("id"), _s = that.utility.selectedSource(_i);
          //  if (!that.selected || (that.selected && that.selected.Id !== _i)) {
                that.selected = { Id: _i, File: _selectedItem.data("file"), Name: _selectedItem.data("name"), Value: _s.Value, SourceType: _s.SourceType };
                that.controls.list.find("li.selected .ckb-wrapper").removeClass("checked");
                that.controls.list.find("li.selected").removeClass("selected");
                that.controls.list.find(".add-point-customformat.show").removeClass("show");
                that.controls.list.find(".add-point-customformat").find("ul .listFormats").removeClass("active");
                _selectedItem.find("ul .listFormats").removeClass("active");

                _selectedItem.addClass("selected");
                _selectedItem.find(".ckb-wrapper").addClass("checked");
                _selectedItem.find(".lbPreviewValue").html(that.selected.Value);
                _selectedItem.find(".btnSelectFormat").prop("original", that.selected.Value);

                if (!$(o).find(".add-point-customformat").hasClass("show")) {
                    $(o).find(".add-point-customformat").addClass("show");

                    that.action.customFormat({ o: _selectedItem, selectedPoint: that.selected });
                }
            //}
        },
        save: function (o) {

            $(o).closest(".add-point-customformat").removeClass("show");
            $(o).closest(".add-point-customformat").find("ul .listFormats").removeClass("active");

            let value = that.selected.SourceType === 2 ? JSON.parse(that.selected.Value).image : that.selected.Value;
            
            // Add value to document
            Office.context.document.setSelectedDataAsync(
                value,
                {
                    coercionType: that.selected.SourceType === 1 ? "text" : "image"
                },
                function (asyncResult) {
                if (asyncResult.status === Office.AsyncResultStatus.Failed) {
                    //console.log(asyncResult.error.message);
                    that.popup.message({ success: false, title: "Adding the source point to the slide failed." });
                }
                else {
                    that.popup.message({ success: true, title: window.stringResources["SourcePointAddedCatalog"] }, function () { that.popup.hide(3000); });
                }
            });
        },
        selectedFormats: function (_that) {
            var _fi = [], _fd = [], _fn = [];
            _that.closest(".listFormats").find("ul > li").each(function (i, d) {
                if ($(this).hasClass("checked")) {
                    _fi.push($(this).data("id"));
                    _fd.push($.trim($(this).data("displayname")));
                    _fn.push($.trim($(this).data("name")));
                    if ($.trim($(_that).data("name")).indexOf("ConvertTo") > -1) {
                        _that.closest(".point-item").find(".btnSelectFormat").prop("place", "");
                    }
                }
            });
            _that.closest(".point-item").find(".btnSelectFormat").html(_fd.length > 0 ? _fd.join(", ") : window.stringResources["None"]);
            _that.closest(".point-item").find(".btnSelectFormat").prop("title", _fd.length > 0 ? _fd.join(", ") : window.stringResources["None"]);
            _that.closest(".point-item").find(".btnSelectFormat").prop("selected", _fi.join(","));
            _that.closest(".point-item").find(".btnSelectFormat").prop("name", _fn.join(","));
            that.format.preview($(_that).closest(".point-item"));
        },
        customFormat: function (options, callback) {
            var _r = false;
            var _pp = false;
            setTimeout(function () {
                if (!_r) {
                    that.popup.processing(true);
                    _pp = true;
                }
            }, 250);

            that.service.customFormat(function (result) {
                _r = true;
                if (_pp) {
                    that.popup.processing(false);
                }
                if (result.status === app.status.succeeded) {
                    if (result.data) {
                        that.ui.customFormat({ o: options.o, data: result.data, selected: options.selected ? options.selected : null, selectedPoint: options.selectedPoint ? options.selectedPoint : null, ref: options.ref }, callback);
                    }
                }
                else {
                    that.ui.customFormat({ o: options.o, selected: options.selected ? options.selected : null, selectedPoint: options.selectedPoint ? options.selectedPoint : null, ref: options.ref }, callback);
                    that.popup.message({ success: false, title: window.stringResources["LoadCustomFormatFailed"] });
                }
            });
        }
    };

    that.document = {
        init: function (callback) {
            that.popup.processing(true);
            that.service.token({ endpoint: that.endpoints.token }, function (result) {
                if (result.status == app.status.succeeded) {
                    that.api.token = result.data;
                    that.api.host = that.utility.path()[0].toLowerCase();
                    that.service.token({ endpoint: that.endpoints.sharePointToken }, function (result) {
                        if (result.status == app.status.succeeded) {
                            that.api.sharePointToken = result.data;
                            that.document.site(null, callback);
                        }
                        else {
                            that.document.error({ title: window.stringResources["AccessTokenFailed"] });
                        }
                    });
                }
                else {
                    that.document.error({ title: window.stringResources["GraphTokenFailed"] });
                }
            });
        },
        site: function (options, callback) {
            if (options == null) {
                options = {
                    path: that.utility.path().reverse(),
                    index: 0,
                    values: [],
                    webUrls: []
                };
            }
            if (options.index < options.path.length) {
                that.service.siteCollection({ path: options.path[options.index] }, function (result) {
                    if (result.status == app.status.succeeded) {
                        options.values.push(result.data.id);
                        options.webUrls.push(result.data.webUrl);
                    }
                    options.index++;
                    that.document.site(options, callback);
                });
            }
            else {
                if (options.values.length > 0) {
                    that.document.library({ siteId: options.values.shift(), siteUrl: options.webUrls.shift() }, callback);
                }
                else {
                    that.document.error({ title: window.stringResources["SiteUrlFailed"] });
                }
            }
        },
        library: function (options, callback) {
            that.service.libraries(options, function (result) {
                if (result.status == app.status.succeeded) {
                    var _l = "";
                    $.each(result.data.value, function (i, d) {
                        if (decodeURI(that.filePath).toUpperCase().indexOf(decodeURI(d.webUrl).toUpperCase()) > -1) {
                            _l = d.name;
                            return false;
                        }
                    });
                    if (_l != "") {
                        that.document.file({ siteId: options.siteId, siteUrl: options.siteUrl, listName: _l, fileName: that.utility.fileName(that.filePath) }, callback);
                    }
                    else {
                        that.document.error({ title: window.stringResources["GetLibraryNameFailed"] });
                    }
                }
                else {
                    that.document.error({ title: window.stringResources["GetLibraryNameFailed"] });
                }
            });
        },
        file: function (options, callback) {
            that.service.item(options, function (result) {
                if (result.status == app.status.succeeded) {
                    var _d = "";
                    $.each(result.data.value, function (i, d) {
                        if (decodeURI(that.filePath).toUpperCase() == decodeURI(d.EncodedAbsUrl).toUpperCase() && d.OData__dlc_DocId) {
                            _d = d.OData__dlc_DocId;
                            return false;
                        }
                    });
                    if (_d != "") {
                        that.selectDocument = { Id: _d, Name: options.fileName };
                        callback();
                    }
                    else {
                        that.document.error({ title: window.stringResources["DocumentIdFailed"] });
                    }
                }
                else {
                    that.document.error({ title: window.stringResources["DocumnetIdFailed"] });
                }
            });
        },
        error: function (options) {
            that.controls.documentIdError.html(window.stringResources["ErrorMessage"] + options.title);
            that.controls.main.addClass("error");
            that.popup.processing(false);
        }
    };

    that.browse = {
        path: [],
        init: function () {
            that.api.token = "";
            that.browse.path = [];
            that.browse.popup.dft();
            that.browse.popup.show();
            that.browse.popup.processing(true);
            that.browse.token();
        },
        token: function () {
            that.service.token({ endpoint: that.endpoints.token }, function (result) {
                if (result.status == app.status.succeeded) {
                    that.api.token = result.data;
                    that.api.host = that.utility.path()[0].toLowerCase();
                    that.service.token({ endpoint: that.endpoints.sharePointToken }, function (result) {
                        if (result.status == app.status.succeeded) {
                            that.api.sharePointToken = result.data;
                            that.browse.siteCollection();
                        }
                        else {
                            that.document.error({ title: "Get sharepoint access token failed." });
                        }
                    });
                }
                else {
                    that.browse.popup.message("Get graph access token failed.");
                }
            });
        },
        siteCollection: function (options) {
            if (typeof (options) == "undefined") {
                options = {
                    path: that.utility.path().reverse(),
                    index: 0,
                    values: [],
                    webUrls: []
                };
            }
            if (options.index < options.path.length) {
                that.service.siteCollection({ path: options.path[options.index] }, function (result) {
                    if (result.status == app.status.succeeded) {
                        if (typeof (result.data.siteCollection) != "undefined") {
                            options.values.push(result.data.id);
                            options.webUrls.push(result.data.webUrl);
                        }
                        options.index++;
                        that.browse.siteCollection(options);
                    }
                    else {
                        if (result.error.status == 401) {
                            that.browse.popup.message("Access denied.");
                        }
                        else {
                            options.index++;
                            that.browse.siteCollection(options);
                        }
                    }
                });
            }
            else {
                if (options.values.length > 0) {
                    that.browse.sites({ siteId: options.values.shift(), siteUrl: options.webUrls.shift() });
                }
                else {
                    that.browse.popup.message("Get site collection ID failed.");
                }
            }
        },
        sites: function (options) {
            that.service.sites(options, function (result) {
                if (result.status == app.status.succeeded) {
                    var _s = [];
                    $.each(result.data.value, function (i, d) {
                        _s.push({ id: d.id, name: d.name, type: "site", siteUrl: d.webUrl });
                    });
                    that.browse.libraries({ siteId: options.siteId, siteUrl: options.siteUrl, sites: _s });
                }
                else {
                    that.browse.popup.message("Get sites failed.");
                }
            });
        },
        libraries: function (options) {
            that.service.libraries(options, function (result) {
                if (result.status == app.status.succeeded) {
                    var _l = options.sites ? options.sites : [];
                    $.each(result.data.value, function (i, d) {
                        _l.push({ id: d.id, name: decodeURI(d.name), type: "library", siteId: options.siteId, siteUrl: options.siteUrl, url: d.webUrl });
                    });
                    that.browse.display({ data: _l });
                }
                else {
                    that.browse.popup.message("Get libraries failed.");
                }
            });
        },
        items: function (options) {
            if (options.inFolder) {
                that.service.itemsInFolder(options, function (result) {
                    if (result.status == app.status.succeeded) {
                        var _fd = [], _fi = [];
                        $.each(result.data.value, function (i, d) {
                            var _u = d.webUrl, _n = d.name, _nu = decodeURI(_n);
                            if (d.folder) {
                                _fd.push({ id: d.id, name: _nu, type: "folder", url: _u, siteId: options.siteId, siteUrl: options.siteUrl, listId: options.listId, listName: options.listName });
                            }
                            else if (d.file) {
                                if (_n.toUpperCase().indexOf(".XLSX") > 0) {
                                    _fi.push({ id: d.id, name: _nu, type: "file", url: _u, siteId: options.siteId, siteUrl: options.siteUrl, listId: options.listId, listName: options.listName });
                                }
                            }
                        });
                        _fi.sort(function (_a, _b) {
                            return (_a.name.toUpperCase() > _b.name.toUpperCase()) ? 1 : (_a.name.toUpperCase() < _b.name.toUpperCase()) ? -1 : 0;
                        });
                        that.browse.display({ data: _fd.concat(_fi) });
                    }
                    else {
                        that.browse.popup.message("Get files failed.");
                    }
                });
            }
            else {
                that.service.items(options, function (result) {
                    if (result.status == app.status.succeeded) {
                        var _fd = [], _fi = [];
                        $.each(result.data.value, function (i, d) {
                            var _u = d.webUrl, _n = d.name, _nu = decodeURI(_n);
                            if (d.folder) {
                                _fd.push({ id: d.id, name: _nu, type: "folder", url: _u, siteId: options.siteId, siteUrl: options.siteUrl, listId: options.listId, listName: options.listName });
                            }
                            else if (d.file) {
                                if (_n.toUpperCase().indexOf(".XLSX") > 0) {
                                    _fi.push({ id: d.id, name: _nu, type: "file", url: _u, siteId: options.siteId, siteUrl: options.siteUrl, listId: options.listId, listName: options.listName });
                                }
                            }
                        });
                        _fi.sort(function (_a, _b) {
                            return (_a.name.toUpperCase() > _b.name.toUpperCase()) ? 1 : (_a.name.toUpperCase() < _b.name.toUpperCase()) ? -1 : 0;
                        });
                        that.browse.display({ data: _fd.concat(_fi) });
                    }
                    else {
                        that.browse.popup.message("Get files failed.");
                    }
                });
            }
        },
        file: function (options) {
            that.service.item(options, function (result) {
                if (result.status == app.status.succeeded) {
                    var _d = "";
                    $.each(result.data.value, function (i, d) {
                        if (decodeURI(options.url).toUpperCase() == decodeURI(d.EncodedAbsUrl).toUpperCase() && d.OData__dlc_DocId) {
                            _d = d.OData__dlc_DocId;
                            return false;
                        }
                    });
                    if (_d != "") {
                        that.browse.popup.hide();
                        that.selectDocument = { Id: _d, Name: options.fileName };
                        that.list();
                    }
                    else {
                        that.browse.popup.message("Get file Document ID failed.");
                    }
                }
                else {
                    that.browse.popup.message("Get file Document ID failed.");
                }
            });
        },
        display: function (options) {
            that.controls.popupBrowseList.html("");
            $.each(options.data, function (i, d) {
                var _h = "";
                if (d.type == "site") {
                    _h = '<li class="i-site" data-id="' + d.id + '" data-type="site" data-siteurl="' + d.siteUrl + '">' + d.name + '</li>';
                }
                else if (d.type == "library") {
                    _h = '<li class="i-library" data-id="' + d.id + '" data-site="' + d.siteId + '" data-url="' + d.url + '" data-type="library" data-siteurl="' + d.siteUrl + '" data-listname="' + d.name + '">' + d.name + '</li>';
                }
                else if (d.type == "folder") {
                    _h = '<li class="i-folder" data-id="' + d.id + '" data-site="' + d.siteId + '" data-list="' + d.listId + '" data-url="' + d.url + '" data-type="folder" data-siteurl="' + d.siteUrl + '" data-listname="' + d.listName + '">' + d.name + '</li>';
                }
                else if (d.type == "file") {
                    _h = '<li class="i-file" data-id="' + d.id + '" data-site="' + d.siteId + '" data-list="' + d.listId + '" data-url="' + d.url + '" data-type="file" data-siteurl="' + d.siteUrl + '" data-listname="' + d.listName + '">' + d.name + '</li>';
                }
                that.controls.popupBrowseList.append(_h);
            });
            if (options.data.length == 0) {
                that.controls.popupBrowseList.html("No items found.");
            }
            that.browse.popup.processing(false);
        },
        select: function (elem) {
            var _t = $(elem).data("type");
            if (_t == "site") {
                that.browse.path.push({ type: "site", id: $(elem).data("id"), siteUrl: $(elem).data("siteurl") });
                that.browse.popup.nav();
                that.browse.popup.processing(true);
                that.browse.sites({ siteId: $(elem).data("id"), siteUrl: $(elem).data("siteurl") });
            }
            else if (_t == "library") {
                that.browse.path.push({ type: "library", id: $(elem).data("id"), site: $(elem).data("site"), url: $(elem).data("url"), siteUrl: $(elem).data("siteurl"), listName: $(elem).data("listname") });
                that.browse.popup.nav();
                that.browse.popup.processing(true);
                that.browse.items({ inFolder: false, siteId: $(elem).data("site"), siteUrl: $(elem).data("siteurl"), listId: $(elem).data("id"), listName: $(elem).data("listname") });
            }
            else if (_t == "folder") {
                that.browse.path.push({ type: "folder", id: $(elem).data("id"), site: $(elem).data("site"), siteUrl: $(elem).data("siteurl"), list: $(elem).data("list"), url: $(elem).data("url"), listName: $(elem).data("listname") });
                that.browse.popup.nav();
                that.browse.popup.processing(true);
                that.browse.items({ inFolder: true, siteId: $(elem).data("site"), siteUrl: $(elem).data("siteurl"), listId: $(elem).data("list"), listName: $(elem).data("listname"), itemId: $(elem).data("id") });
            }
            else {
                that.browse.popup.processing(true);
                that.browse.file({ siteUrl: $(elem).data("siteurl"), listName: $(elem).data("listname"), name: $(elem).text(), url: that.browse.path[that.browse.path.length - 1].url + "/" + encodeURI($(elem).text()), fileName: $.trim($(elem).text()) });
            }
        },
        popup: {
            dft: function () {
                that.controls.popupBrowseList.html("");
                that.controls.popupBrowseBack.hide();
                that.controls.popupBrowseMessage.html("").hide();
                that.controls.popupBrowseLoading.hide();
            },
            show: function () {
                that.controls.popupMain.removeClass("message process confirm").addClass("active browse");
            },
            hide: function () {
                that.controls.popupMain.removeClass("active message process confirm browse");
            },
            processing: function (show) {
                if (show) {
                    that.controls.popupBrowseLoading.show();
                }
                else {
                    that.controls.popupBrowseLoading.hide();
                }
            },
            message: function (txt) {
                that.controls.popupBrowseMessage.html(txt).show();
                that.browse.popup.processing(false);
            },
            back: function () {
                that.browse.path.pop();
                if (that.browse.path.length > 0) {
                    var _ip = that.browse.path[that.browse.path.length - 1];
                    if (_ip.type == "site") {
                        that.browse.popup.processing(true);
                        that.browse.sites({ siteId: _ip.id, siteUrl: _ip.siteUrl });
                    }
                    else if (_ip.type == "library") {
                        that.browse.popup.processing(true);
                        that.browse.items({ inFolder: false, siteId: _ip.site, listId: _ip.id, siteUrl: _ip.siteUrl, listName: _ip.listName });
                    }
                    else if (_ip.type == "folder") {
                        that.browse.popup.processing(true);
                        that.browse.items({ inFolder: true, siteId: _ip.site, listId: _ip.list, itemId: _ip.id, siteUrl: _ip.siteUrl, listName: _ip.listName });
                    }
                }
                else {
                    that.browse.popup.processing(true);
                    that.browse.siteCollection();
                }
                that.browse.popup.nav();
            },
            nav: function () {
                that.browse.path.length > 0 ? that.controls.popupBrowseBack.show() : that.controls.popupBrowseBack.hide();
            }
        }
    };

    that.format = {
        convert: function (options) {
            var _t = $.trim(options.value != null ? options.value : ""),
                _v = _t,
                _f = options.formats ? options.formats : [],
                _d = that.format.hasDollar(_v),
                _c = true, //that.format.hasComma(_v),
                _p = that.format.hasPercent(_v),
                _k = that.format.hasParenthesis(_v),
                _m = [window.stringResources["January"], window.stringResources["February"], window.stringResources["March"],
                window.stringResources["April"], window.stringResources["May"], window.stringResources["June"],
                window.stringResources["July"], window.stringResources["August"], window.stringResources["September"],
                window.stringResources["October"], window.stringResources["November"], window.stringResources["December"]],
                _x = options.decimal;
            $.each(_f, function (_a, _b) {
                if (!_b.IsDeleted) {
                    if (_b.Name == "ConvertToHundreds") {
                        if (that.format.isNumber(_v)) {
                            var _l = that.format.getDecimalLength(_v);
                            _v = new BigNumber(that.format.toNumber(_v)).div(100).toString();
                            _v = that.format.addDecimal(_v, _l);
                            if (_c) {
                                _v = that.format.addComma(_v);
                            }
                            if (_p) {
                                _v = that.format.addPercent(_v);
                            }
                            if (_k) {
                                _v = "(" + _v + ")";
                            }
                            if (_d) {
                                _v = that.format.addDollar(_v);
                            }
                        }
                    }
                    else if (_b.Name == "ConvertToThousands") {
                        if (that.format.isNumber(_v)) {
                            var _l = that.format.getDecimalLength(_v);
                            _v = new BigNumber(that.format.toNumber(_v)).div(1000).toString();
                            _v = that.format.addDecimal(_v, _l);
                            if (_c) {
                                _v = that.format.addComma(_v);
                            }
                            if (_p) {
                                _v = that.format.addPercent(_v);
                            }
                            if (_k) {
                                _v = "(" + _v + ")";
                            }
                            if (_d) {
                                _v = that.format.addDollar(_v);
                            }
                        }
                    }
                    else if (_b.Name == "ConvertToMillions") {
                        if (that.format.isNumber(_v)) {
                            var _l = that.format.getDecimalLength(_v);
                            _v = new BigNumber(that.format.toNumber(_v)).div(1000000).toString();
                            _v = that.format.addDecimal(_v, _l);
                            if (_c) {
                                _v = that.format.addComma(_v);
                            }
                            if (_p) {
                                _v = that.format.addPercent(_v);
                            }
                            if (_k) {
                                _v = "(" + _v + ")";
                            }
                            if (_d) {
                                _v = that.format.addDollar(_v);
                            }
                        }
                    }
                    else if (_b.Name == "ConvertToBillions") {
                        if (that.format.isNumber(_v)) {
                            var _l = that.format.getDecimalLength(_v);
                            _v = new BigNumber(that.format.toNumber(_v)).div(1000000000).toString();
                            _v = that.format.addDecimal(_v, _l);
                            if (_c) {
                                _v = that.format.addComma(_v);
                            }
                            if (_p) {
                                _v = that.format.addPercent(_v);
                            }
                            if (_k) {
                                _v = "(" + _v + ")";
                            }
                            if (_d) {
                                _v = that.format.addDollar(_v);
                            }
                        }
                    }
                    else if (_b.Name == "ShowNegativesAsPositives") {
                        var _h = that.format.hasDollar(_v),
                            _pt = that.format.hasPercent(_v),
                            _pk = that.format.hasParenthesis(_v),
                            _hh = _v.toString().indexOf("hundred") > -1,
                            _ht = _v.toString().indexOf("thousand") > -1,
                            _hm = _v.toString().indexOf("million") > -1,
                            _hb = _v.toString().indexOf("billion") > -1;
                        _v = $.trim(_v.toString().replace(/\$/g, "").replace(/-/g, "").replace(/%/g, "").replace(/\(/g, "").replace(/\)/g, "").replace(/hundred/g, "").replace(/thousand/g, "").replace(/million/g, "").replace(/billion/g, ""));
                        if (_pt) {
                            _v = that.format.addPercent(_v);
                        }
                        if (_h) {
                            _v = that.format.addDollar(_v);
                        }
                        if (_hh) {
                            _v = _v + " hundred";
                        }
                        else if (_ht) {
                            _v = _v + " thousand";
                        }
                        else if (_hm) {
                            _v = _v + " million";
                        }
                        else if (_hb) {
                            _v = _v + " billion";
                        }
                    }
                    else if (_b.Name == "IncludeHundredDescriptor") {
                        if (that.format.isNumber(_v)) {
                            _v = _v + " hundred";
                        }
                    }
                    else if (_b.Name == "IncludeThousandDescriptor") {
                        if (that.format.isNumber(_v)) {
                            _v = _v + " thousand";
                        }
                    }
                    else if (_b.Name == "IncludeMillionDescriptor") {
                        if (that.format.isNumber(_v)) {
                            _v = _v + " million";
                        }
                    }
                    else if (_b.Name == "IncludeBillionDescriptor") {
                        if (that.format.isNumber(_v)) {
                            _v = _v + " billion";
                        }
                    }
                    else if (_b.Name == "IncludeDollarSymbol") {
                        if (!that.format.hasDollar(_v)) {
                            _v = that.format.addDollar(_v);
                        }
                    }
                    else if (_b.Name == "ExcludeDollarSymbol") {
                        if (that.format.hasDollar(_v)) {
                            _v = that.format.removeDollar(_v);
                        }
                    }
                    else if (_b.Name == "DateShowLongDateFormat") {
                        if (that.format.isDate(_v)) {
                            var _tt = new Date(_v);
                            _v = _m[_tt.getMonth()] + " " + _tt.getDate() + ", " + _tt.getFullYear();
                        }
                    }
                    else if (_b.Name == "DateShowYearOnly") {
                        if (that.format.isDate(_v)) {
                            var _tt = new Date(_v);
                            _v = _tt.getFullYear();
                        }
                    }
                    else if (_b.Name == "ConvertNegativeSymbolToParenthesis") {
                        var _h = that.format.hasDollar(_v),
                            _pt = that.format.hasPercent(_v),
                            _hh = _v.toString().indexOf("hundred") > -1,
                            _ht = _v.toString().indexOf("thousand") > -1,
                            _hm = _v.toString().indexOf("million") > -1,
                            _hb = _v.toString().indexOf("billion") > -1;
                        if (_v.indexOf("-") > -1) {
                            _v = $.trim(_v.toString().replace(/\$/g, "").replace(/-/g, "").replace(/%/g, "").replace(/\(/g, "").replace(/\)/g, "").replace(/hundred/g, "").replace(/thousand/g, "").replace(/million/g, "").replace(/billion/g, ""));
                            if (_h) {
                                _v = that.format.addDollar(_v);
                            }
                            _v = "(" + _v + ")";
                            if (_pt) {
                                _v = that.format.addPercent(_v);
                            }
                            if (_hh) {
                                _v = _v + " hundred";
                            }
                            else if (_ht) {
                                _v = _v + " thousand";
                            }
                            else if (_hm) {
                                _v = _v + " million";
                            }
                            else if (_hb) {
                                _v = _v + " billion";
                            }
                        }
                    }
                }
            });
            if (_x != null && _x.toString() != "") {
                var __a = that.format.hasComma(_v), __b = that.format.remove(_v);
                if (that.format.isNumber(__b)) {
                    var __c = __a ? that.format.addComma(__b) : __b, __d = "" + new BigNumber(__b).toFixed(_x) + "", __e = __a ? that.format.addComma(__d) : __d;
                    _v = _v.replace(__c, __e)
                }
            }
            return _v;
        },
        toNumber: function (_v) {
            return that.format.removeComma(that.format.removeDollar(that.format.removePercent(that.format.removeParenthesis(_v))));
        },
        isNumber: function (_v) {
            return that.format.toNumber(_v) != "" && !isNaN(that.format.toNumber(_v));
        },
        isDate: function (_v) {
            var _ff = false;
            try {
                _ff = _ff = (new Date(_v.toString().replace(/ /g, ""))).getFullYear() > 0;
            } catch (e) {
                _ff = false;
            }
            return _ff;
        },
        hasDollar: function (_v) {
            return _v.toString().indexOf("$") > -1;
        },
        hasComma: function (_v) {
            return _v.toString().indexOf(",") > -1;
        },
        removeDollar: function (_v) {
            return _v.toString().replace("$", "");
        },
        addDollar: function (_v) {
            return "$" + _v;
        },
        removeComma: function (_v) {
            return _v.toString().replace(/,/g, "");
        },
        addComma: function (_v) {
            var __s = _v.toString().split(".");
            __s[0] = __s[0].replace(new RegExp('(\\d)(?=(\\d{3})+$)', 'ig'), "$1,");
            return __s.join(".");
        },
        removePercent: function (_v) {
            return _v.toString().replace(/%/g, "");
        },
        hasPercent: function (_v) {
            return _v.toString().indexOf("%") > -1;
        },
        addPercent: function (_v) {
            return _v + "%";
        },
        hasParenthesis: function (_v) {
            return _v.toString().indexOf("(") > -1 && _v.toString().indexOf(")") > -1;
        },
        removeParenthesis: function (_v) {
            return _v.toString().replace(/\(/g, "").replace(/\)/g, "");
        },
        remove: function (_v) {
            return $.trim(_v.toString().replace(/\$/g, "").replace(/,/g, "").replace(/-/g, "").replace(/%/g, "").replace(/\(/g, "").replace(/\)/g, "").replace(/hundred/g, "").replace(/thousand/g, "").replace(/million/g, "").replace(/billion/g, ""));
        },
        getDecimalLength: function (_v) {
            var _a = _v.toString().replace(/\$/g, "").replace(/,/g, "").replace(/-/g, "").replace(/\(/g, "").replace(/\)/g, "").split(".");
            if (_a.length == 2) {
                return _a[1].length;
            }
            else {
                return 0;
            }
        },
        addDecimal: function (_v, _l) {
            var _dl = that.format.getDecimalLength(_v);
            if (_l > 0 && _dl == 0) {
                _v = "" + new BigNumber(_v).toFixed(_l) + "";
            }
            return _v;
        },
        preview: function (o) {
            var _v = o.find(".btnSelectFormat").prop("original");
            var _n = o.find(".btnSelectFormat").prop("name");
            var _f = (typeof (_n) != "undefined" && _n != "") ? _n.split(",") : [], _fa = [];
            var _x = o.find(".btnSelectFormat").prop("place");
            $.each(_f, function (_a, _b) {
                _fa.push({ Name: _b });
            });
            var _fd = that.format.convert({ value: _v, formats: _fa, decimal: _x });
            o.find(".lbPreviewValue").html(_fd);
            that.selected.Value = _fd;
        }
    };

    that.popup = {
        message: function (options, callback) {
            $(".popups .bg").removeAttr("style");
            if (options.success) {
                that.controls.popupMessage.removeClass("error").addClass("success");
                // that.controls.popupSuccessMessage.html(options.title);
                that.controls.innerMessageBox.removeClass("active ms-MessageBar--error").addClass("active ms-MessageBar--success");
                that.controls.innerMessageIcon.removeClass("ms-Icon--ErrorBadge").addClass("ms-Icon--Completed");
                that.controls.innerMessageText.html(options.title);
                $(".popups .bg").hide();
            }
            else {
                if (options.values) {
                    that.controls.popupMessage.removeClass("success").addClass("error");
                    that.controls.popupErrorTitle.html(options.title ? options.title : "");
                    var _s = "error-single";

                    _s = "error-list";
                    var _h = "";
                    $.each(options.values, function (i, d) {
                        _h += "<li>";
                        $.each(d, function (m, n) {
                            _h += "<span>" + n + "</span>";
                        });
                        _h += "</li>";
                    });
                    that.controls.popupErrorMessage.html(_h);
                    if (options.repair) {
                        _s = "error-repair";
                        that.controls.popupErrorRepair.html(options.repair);
                    }

                    that.controls.popupErrorMain.removeClass("error-single error-list").addClass(_s);
                }
                else {
                    that.controls.popupMessage.removeClass("error").addClass("success");
                    // that.controls.popupSuccessMessage.html(options.title);
                    that.controls.innerMessageBox.removeClass("active ms-MessageBar--success").addClass("active ms-MessageBar--error");
                    that.controls.innerMessageIcon.removeClass("ms-Icon--Completed").addClass("ms-Icon--ErrorBadge");
                    that.controls.innerMessageText.html(options.title);
                    $(".popups .bg").hide();
                }
            }
            if (options.canClose) {
                that.controls.innerMessageBox.addClass("canclose");
            }
            else {
                that.controls.innerMessageBox.removeClass("canclose");
            }
            that.controls.popupMain.removeClass("process confirm browse").addClass("active message");
            if (options.success) {
                callback();
            }
            else {
                if (options.values) {
                    that.controls.popupErrorOK.unbind("click").click(function () {
                        that.action.ok();
                        if (callback) {
                            callback();
                        }
                    });
                }
                else {
                    if (callback) {
                        callback();
                    }
                    else {
                        that.popup.hide(3000);
                    }
                }
            }
        },
        processing: function (show) {
            if (!show) {
                that.controls.popupMain.removeClass("active process");
            }
            else {
                that.controls.popupMain.removeClass("message confirm browse").addClass("active process");
            }
        },
        browse: function (show) {
            if (!show) {
                that.controls.popupMain.removeClass("active browse");
            }
            else {
                that.controls.popupMain.removeClass("message process confirm").addClass("active browse");
            }
        },
        hide: function (millisecond) {
            if (millisecond) {
                setTimeout(function () {
                    $(".popups .bg").removeAttr("style");
                    that.controls.popupMain.removeClass("active message");
                    that.controls.innerMessageBox.removeClass("active");
                }, millisecond);
            } else {
                $(".popups .bg").removeAttr("style");
                that.controls.popupMain.removeClass("active message");
                that.controls.innerMessageBox.removeClass("active");
            }
        },
        back: function (millisecond) {
            if (millisecond) {
                setTimeout(function () {
                    that.controls.popupMain.removeClass("active message");
                    that.controls.main.removeClass("manage add").addClass("manage");
                }, millisecond);
            }
            else {
                that.controls.popupMain.removeClass("active message");
                that.controls.main.removeClass("manage add").addClass("manage");
            }
        }
    };

    that.service = {
        common: function (options, callback) {
            if (!that.token)
                return;

            let apiToken = that.token;
            let apiHeaders = { "authorization": "Bearer " + apiToken };

            $.ajax({
                url: options.url,
                type: options.type,
                cache: false,
                data: options.data ? options.data : "",
                dataType: options.dataType,
                headers: options.headers ? options.headers : apiHeaders,
                success: function (data) {
                    callback({ status: app.status.succeeded, data: data });
                },
                error: function (error) {
                    if (error.status == 410) {
                        that.popup.message({ success: false, title: "The current login gets expired and needs re-authenticate. You will be redirected to the login page by click OK." }, function () {
                            //window.location = "/Home/Index";
                        });
                    }
                    else {
                        callback({ status: app.status.failed, error: error });
                    }
                }
            });
        },
        catalog: function (options, callback) {
            that.service.common({ url: that.endpoints.catalog + options.documentId, type: "GET", dataType: "json" }, callback);
        },
        token: function (options, callback) {
            that.service.common({ url: options.endpoint, type: "GET", dataType: "json" }, callback);
        },
        siteCollection: function (options, callback) {
            that.service.common({ url: that.endpoints.graph + "/sites/" + that.api.host + ":/" + options.path, type: "GET", dataType: "json", headers: { "authorization": "Bearer " + that.api.token } }, callback);
        },
        sites: function (options, callback) {
            that.service.common({ url: that.endpoints.graph + "/sites/" + options.siteId + "/sites", type: "GET", dataType: "json", headers: { "authorization": "Bearer " + that.api.token } }, callback);
        },
        libraries: function (options, callback) {
            that.service.common({ url: that.endpoints.graph + "/sites/" + options.siteId + "/drives", type: "GET", dataType: "json", headers: { "authorization": "Bearer " + that.api.token } }, callback);
        },
        items: function (options, callback) {
            that.service.common({ url: that.endpoints.graph + "/sites/" + options.siteId + "/drives/" + options.listId + "/root/children", type: "GET", dataType: "json", headers: { "authorization": "Bearer " + that.api.token } }, callback);
        },
        itemsInFolder: function (options, callback) {
            that.service.common({ url: that.endpoints.graph + "/sites/" + options.siteId + "/drives/" + options.listId + "/items/" + options.itemId + "/children", type: "GET", dataType: "json", headers: { "authorization": "Bearer " + that.api.token } }, callback);
        },
        item: function (options, callback) {
            that.service.common({ url: options.siteUrl + "/_api/web/lists/getbytitle('" + options.listName + "')/items?$select=FileLeafRef,EncodedAbsUrl,OData__dlc_DocId&$filter=FileLeafRef eq '" + options.fileName + "'", type: "GET", dataType: "json", headers: { "authorization": "Bearer " + that.api.sharePointToken } }, callback);
        },
        userInfo: function (callback) {
            that.service.common({ url: that.endpoints.userInfo, type: "GET" }, callback);
        },
        customFormat: function (callback) {
            that.service.common({ url: that.endpoints.customFormat, type: "GET", dataType: "json" }, callback);
        }
    };

    that.ui = {
        dft: function () {
            var _f = $.trim(that.controls.file.val()), _fd = that.controls.file.data("default"), _k = $.trim(that.controls.keyword.val()), _kd = that.controls.keyword.data("default");
            if (_f == "" || _f == _fd) {
                that.controls.file.val(_fd);
            }
            if (_k == "" || _k == _kd) {
                that.controls.keyword.val(_kd).addClass("input-default");
                that.controls.search.removeClass("ms-Icon--Search ms-Icon--Cancel").addClass("ms-Icon--Search");
            }
        },
        list: function (options) {
            var _dt = $.extend([], that.points), _d = [], _ss = [];
            if (that.sourcePointKeyword != "") {
                var _sk = app.search.splitKeyword({ keyword: that.sourcePointKeyword });
                if (_sk.length > 26) {
                    that.popup.message({ success: false, title: "Only support less then 26 keywords." });
                }
                else {
                    $.each(_dt, function (i, d) {
                        if (app.search.weight({ keyword: _sk, source: d }) > 0) {
                            _d.push(d);
                        }
                    });
                }
            }
            else {
                _d = _dt;
            }
            that.utility.pager.status({ length: _d.length });
            _d.sort(function (_a, _b) {
                return (app.string(_a.Name).toUpperCase() > app.string(_b.Name).toUpperCase()) ? 1 : (app.string(_a.Name).toUpperCase() < app.string(_b.Name).toUpperCase()) ? -1 : 0;
            });
            that.controls.list.find(".point-item").remove();
            that.ui.item({ index: 0, data: _d, selected: _ss });
        },
        item: function (options, callback) {
            that.utility.scrollTop(0);
            that.controls.list.html("");

            let pointCounter = 0;
            let tableCounter = 0;
            let chartCounter = 0;

            let selectedSourceType = that.utility.selectedSourceType(".point-types");

            selectedSourceType === app.sourceTypes.all ? app.sourceTypes.point : selectedSourceType;

            $.each(options.data,
                function (index, d)
                {
                    if (d.SourceType === app.sourceTypes.point) {
                        pointCounter++;
                    } else if (d.SourceType === app.sourceTypes.table) {
                        tableCounter++;
                    } else {
                        chartCounter++;
                    }

                    if (d.SourceType === selectedSourceType)
                    {
                        var _pn = that.utility.position(d.Position);
                        var _h = '<li class="point-item" data-id="' + d.Id + '" data-range="' + d.RangeId + '" data-position="' + d.Position + '" data-namerange="' + d.NameRangeId + '" data-nameposition="' + d.NamePosition + '">';

                        _h += '<div class="point-item-line">';
                        _h += '<div class="i2"><span class="s-name" title="' + d.Name + '">' + d.Name + '</span>';
                        _h += '<div class="sp-file-pos">';
                        _h += '<span title="' + (_pn.sheet ? _pn.sheet : "") + ':[' + (_pn.cell ? _pn.cell : "") + ']">' + (_pn.sheet ? _pn.sheet : "") + ':[' + (_pn.cell ? _pn.cell : "") + ']</span>';
                        _h += '</div>';
                        _h += '</div>';

                        if (d.SourceType === app.sourceTypes.point) {
                            _h += '<div class="i3" title="' + d.Value + '">' + d.Value + '</div>';
                        }

                        _h += '</div>'; //point-item-line

                        /* Edit format */
                        _h += '<div class="add-point-customformat" style="margin-left: 5% !important">';

                        if (d.SourceType === app.sourceTypes.point) {
                            _h += '<span class="i-preview">Preview: <strong class="lbPreviewValue"></strong></span>';
                            _h += '<div class="addCustomFormat add-point-format">';

                            _h += '<div class="add-point-place">';
                            _h += '<span class="i-decimal">Decimal place: ';
                            _h += '<i class="i-increase" title="Increase decimal places"></i><i class="i-decrease" title="Decrease decimal places"></i></span>';
                            _h += '</div>';

                            _h += '<div class="add-point-box">';
                            _h += '<div class="add-point-select">';
                            _h += '<a class="btnSelectFormat" href="javascript:"></a>';
                            _h += '<i class="iconSelectFormat ms-Icon ms-Icon--ChevronDown"></i>';
                            _h += '<ul class="listFormats">';
                            _h += '</ul>';
                            _h += '</div>';
                        }

                        _h += '<button class="ms-Button ms-Button--small ms-Button--primary i-add">';
                        _h += '<span class="ms-Button-label">' + window.stringResources["Add"] + '</span>';
                        _h += '</button>';

                        _h += '</div>';
                        _h += '</div>';
                        _h += '<div class="clear"></div>';
                        _h += '</div>';
                        /* Edit format end */

                        _h += '</li>';
                        that.controls.list.append(_h);
                    }
                });

            // Update counters
            that.controls.sourceTypeNav[0].children[1].innerText = pointCounter;
            that.controls.sourceTypeNav[1].children[1].innerText = chartCounter;
            that.controls.sourceTypeNav[2].children[1].innerText = tableCounter;

            that.controls.headerListPoints.find(".i3 span")[0].innerText = selectedSourceType === app.sourceTypes.point ? window.stringResources["Value"] : "";
        },
        customFormat: function (options, callback) {
            var _currentItem = options.o.hasClass("point-item") ? options.o : options.o.closest(".point-item");

            _currentItem.find(".listFormats").html("");
            var _si = [], _sn = [], _sd = [];
            if (options.selected && options.selected != null) {
                $.each(options.selected.CustomFormats, function (_x, _y) {
                    _si.push(_y.Id);
                    _sn.push(_y.Name);
                    _sd.push(_y.DisplayName);
                });
                _currentItem.find(".btnSelectFormat").html(_sd.length > 0 ? _sd.join(", ") : window.stringResources["None"]);
                _currentItem.find(".btnSelectFormat").prop("title", _sd.length > 0 ? _sd.join(", ") : window.stringResources["None"]);
                _currentItem.find(".btnSelectFormat").prop("selected", _si.join(","));
                _currentItem.find(".btnSelectFormat").prop("name", _sn.join(","));
                _currentItem.find(".btnSelectFormat").prop("place", options.selected.DecimalPlace && options.selected.DecimalPlace != null ? options.selected.DecimalPlace : "");
            }
            else {
                _currentItem.find(".btnSelectFormat").html(window.stringResources["None"]);
                _currentItem.find(".btnSelectFormat").prop("title", window.stringResources["None"]);
                _currentItem.find(".btnSelectFormat").prop("selected", "");
                _currentItem.find(".btnSelectFormat").prop("name", "");
                _currentItem.find(".btnSelectFormat").prop("place", "");
            }

            var _v = options.ref ? options.selected.ReferencedSourcePoint.Value : options.selectedPoint.Value; //_dataList.find("li." + _itemSelectedName + " .btnSelectFormat").prop("original");
            _currentItem.find(".listFormats").removeClass("convert1 convert2 convert3 convert4");
            _currentItem.find(".lbPreviewValue").html(_v);
            _currentItem.find(".addCustomFormat").removeClass("selected-number selected-date");
            if (that.format.isNumber(_v)) {
                _currentItem.find(".addCustomFormat").addClass("selected-number");
            }
            else if (that.format.isDate(_v)) {
                _currentItem.find(".addCustomFormat").addClass("selected-date");
            }

            if (_sn.length > 0) {
                var _tn = $.inArray("ConvertToThousands", _sn) > -1 ? "IncludeThousandDescriptor" : ($.inArray("ConvertToMillions", _sn) > -1 ? "IncludeMillionDescriptor" : ($.inArray("ConvertToBillions", _sn) > -1 ? "IncludeBillionDescriptor" : ($.inArray("ConvertToHundreds", _sn) > -1 ? "IncludeHundredDescriptor" : "")));
                var _cl = $.inArray("ConvertToThousands", _sn) > -1 ? "convert2" : ($.inArray("ConvertToMillions", _sn) > -1 ? "convert3" : ($.inArray("ConvertToBillions", _sn) > -1 ? "convert4" : ($.inArray("ConvertToHundreds", _sn) > -1 ? "convert1" : "")));
                _currentItem.find(".listFormats").addClass(_cl);
                _currentItem.find(".listFormats").find("ul > li[data-name=" + _tn + "]").addClass("checked");
            }

            var _dd = [], _dt = [];
            $.each(options.data, function (_i, _e) {
                var _i = $.inArray(_e.GroupName, _dt);
                if (_i == -1) {
                    _dd.push({ Name: _e.GroupName, OrderBy: _e.GroupOrderBy, Formats: [{ Id: _e.Id, Name: _e.Name, DisplayName: _e.DisplayName, Description: _e.Description, OrderBy: _e.OrderBy }] });
                    _dt.push(_e.GroupName);
                }
                else {
                    _dd[_i].Formats.push({ Id: _e.Id, Name: _e.Name, DisplayName: _e.DisplayName, Description: _e.Description, OrderBy: _e.OrderBy });
                }
            });
            $.each(_dd, function (_i, _e) {
                _e.Formats.sort(function (_m, _n) {
                    return _m.OrderBy > _n.OrderBy ? 1 : _m.OrderBy < _n.OrderBy ? -1 : 0;
                });
            });
            _dd.sort(function (_m, _n) {
                return _m.OrderBy > _n.OrderBy ? 1 : _m.OrderBy < _n.OrderBy ? -1 : 0;
            });

            if (_dd) {
                $.each(_dd, function (m, n) {
                    var _h = '', _c = '';
                    _c = (n.Name == "Convert to" || n.Name == "Negative number" || n.Name == "Descriptor") ? "value-number" : (n.Name == "Symbol") ? "value-string" : "value-date";
                    _h += '<li class="' + (n.Name == "Descriptor" ? "drp-checkbox drp-descriptor " : "drp-radio ") + '' + _c + '">';
                    if (n.Name != "Descriptor") {
                        _h += '<label>' + n.Name + '</label>';
                    }
                    _h += '<ul>';
                    $.each(n.Formats, function (i, d) {
                        _h += '<li data-id="' + d.Id + '" data-name="' + d.Name + '" title="' + d.Description + '" class="' + ($.inArray(d.Id, _si) > -1 ? "checked" : "") + '" data-displayname="' + d.DisplayName.replace(/"/g, "&quot;") + '">';
                        _h += '<div><i></i></div>';
                        _h += '<a href="javascript:">' + (n.Name == "Descriptor" ? "Descriptor" : d.DisplayName) + '</a>';
                        _h += '</li>';
                    });
                    _h += '</ul>';
                    _h += '</li>';
                    _currentItem.find(".listFormats").append(_h);
                });
            }
            if (callback) {
                callback();
            }
        }
    };

    return that;
})();
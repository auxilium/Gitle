$.ajaxUploadSettings.name = 'uploads';

$.fn.upload = function () {
  return this.each(function () {
    var textarea = $(this);
    var progressbar = $('<div class="progress">').append($('<span class="meter">')).insertAfter(this).hide();
    var uploadButton;
    if (!$.browser.msie) {
      if ($.cookie("culture") === "en-GB") {
        textarea.after($(
          '<small class="info">You can also upload pictures (jpg, png, gif) and files (doc, docx, xls, xlsx, pdf, txt) by dragging them to the text field.</small>'));
      } else {
        textarea.after($(
          '<small class="info">Je kunt ook afbeeldingen (jpg, png, gif) en bestanden (doc, docx, xls, xlsx, pdf, txt) uploaden door ze op het textveld te slepen.</small>'));
      }
    }
    if ($.cookie("culture") === "en-GB") {
      uploadButton = $('<a href="#" class="button no-margin tiny">Upload picture/file</a>')
        .insertBefore(this);
    } else {
      uploadButton = $('<a href="#" class="button no-margin tiny">Upload afbeelding/bestand</a>')
        .insertBefore(this);
    }
    var uploadOptions = {
      url: '/upload/file',
      beforeSend: function (e, a) {
        progressbar.show();
      },
      onprogress: function (e) {
        if (e.lengthComputable) {
          console.log('Progress ' + e.loaded + ' of ' + e.total);
          var percent = e.loaded / e.total * 100;
          progressbar.find('.meter').css('width', percent + '%');
        }
      },
      error: function () {
        console.log('error');
        textarea.error('Bestand(en) uploaden niet gelukt');
      },
      success: function (data) {
        data = $.parseJSON(data);
        for (var upload in data.Uploads) {
          textarea.insert(data.Uploads[upload]);
        }
        for (var error in data.Errors) {
          console.log('Error: ' + error + ' - ' + data.Errors[error]);
          textarea.error(error + ': ' + data.Errors[error]);
        }
        progressbar.hide();
      }
    };
    $(this).ajaxUploadDrop(uploadOptions);
    uploadButton.ajaxUploadPrompt(uploadOptions);
  });
};

$.fn.uploadList = function() {
  return this.each(function () {
    var list = $(this);
    var templateContainer = $($(this).data('template-container'));
    var template = $($(this).data('template'));
    var url = $(this).data('url');
    var progressbar = $('<div class="progress">').append($('<span class="meter">')).insertAfter(this).hide();
    var uploadButton;
    if (!$.browser.msie) {
      if ($.cookie("culture") === "en-GB") {
        list.after($(
          '<small class="info">You can also upload pictures (jpg, png, gif) and files (doc, docx, xls, xlsx, pdf, txt) by dragging them to the text field.</small>'));
      } else {
        list.after($(
          '<small class="info">Je kunt ook afbeeldingen (jpg, png, gif) en bestanden (doc, docx, xls, xlsx, pdf, txt) uploaden door ze op het textveld te slepen.</small>'));
      }
    }
    if ($.cookie("culture") === "en-GB") {
      uploadButton = $('<a href="#" class="button no-margin tiny">Upload picture/file</a>')
        .insertBefore(this);
    } else {
      uploadButton = $('<a href="#" class="button no-margin tiny">Upload afbeelding/bestand</a>')
        .insertBefore(this);
    }
    var uploadOptions = {
      url: url,
      beforeSend: function (e, a) {
        progressbar.show();
      },
      onprogress: function (e) {
        if (e.lengthComputable) {
          console.log('Progress ' + e.loaded + ' of ' + e.total);
          var percent = e.loaded / e.total * 100;
          progressbar.find('.meter').css('width', percent + '%');
        }
      },
      error: function () {
        console.log('error');
        list.error('Bestand(en) uploaden niet gelukt');
      },
      success: function (data) {
        data = $.parseJSON(data);
        for (var upload in data.Uploads) {
          var document = data.Uploads[upload];
          var templateHtml = template.html();
          templateHtml = templateHtml.replace(/{{id}}/g, document.Id);
          templateHtml = templateHtml.replace(/{{name}}/g, document.Name);
          templateHtml = templateHtml.replace(/{{path}}/g, document.Path);
          templateHtml = templateHtml.replace(/{{date}}/g, document.DateString);
          templateHtml = templateHtml.replace(/{{uploader}}/g, document.UserFullName);
          templateContainer.append(templateHtml);
        }
        for (var error in data.Errors) {
          console.log('Error: ' + error + ' - ' + data.Errors[error]);
          list.error(error + ': ' + data.Errors[error]);
        }
        progressbar.hide();
      }
    };
    $(this).ajaxUploadDrop(uploadOptions);
    uploadButton.ajaxUploadPrompt(uploadOptions);
  });
};

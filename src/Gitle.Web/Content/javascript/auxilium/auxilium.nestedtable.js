$.fn.nestedTable = function (conf) {

  var config = jQuery.extend({
    deleteText: 'Verwijder',
    afterAdd: function (row) { }
  }, conf);

  var deleteRow = function (e) {
    e.preventDefault();

    if ($('#addContactId') && $('#addContactId').length > 0) {
      $('#addContactId').append('<option value="' + $(this).parents('tr').find('input[name$=".Id"]').val() +'">'+ $(this).parents('tr').find('input[name$=".FullName"]').val() +'</option>');
    }

    $(this).parents('tr').remove();
  };

  return this.each(function () {

    var table = $(this);

    var deleteHeader = $('<th>' + config.deleteText + '</th>');
    table.find('thead tr').append(deleteHeader);
    var deleteButton = $('<a href="#" class="button tiny no-margin alert">Verwijder</a>');
    deleteButton.click(deleteRow);
    var deleteCell = $('<td></td>').append(deleteButton);
    table.find('tbody tr').append(deleteCell);

    var addRow = $('<a href="#" class="button small">Nieuw</a>');
    addRow.click(function (e) {
      e.preventDefault();
      var fr = table.find('tbody tr:last-child input:first-child');
      var newRowNumber = parseInt(fr.length == 0 ? 0 : fr.prop('name').match(/\d+/)[0]) + 1;
      var firstRow = table.find('tbody tr:first-child'); // use first row to clone
      var newRow = firstRow.clone().removeClass('hide');

      // rename all inputs
      newRow.find(':input').each(function () {
        var input = $(this);
        var name = input.prop('name');
        var newName = name.replace(/\d+/g, newRowNumber);
        input.attr('name', newName);
      });

      newRow.find('[type=text]').val(''); // set all values to nothing
      newRow.find('[data-nested-remove]').remove();
      newRow.find('[type=checkbox]').removeProp('checked'); // uncheck checkbox
      newRow.find('.button.alert').click(deleteRow); // set delete action on delete button

      table.find('tbody').append(newRow); // append new row
      config.afterAdd(newRow); // trigger the afterAdd callback from config
    });
    table.after(addRow);
  });
};

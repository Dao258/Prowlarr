import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import TableOptionsModal from 'Components/Table/TableOptions/TableOptionsModal';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import VirtualTableSelectAllHeaderCell from 'Components/Table/VirtualTableSelectAllHeaderCell';
import { icons } from 'Helpers/Props';
import styles from './SearchIndexHeader.css';

class SearchIndexHeader extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isTableOptionsModalOpen: false
    };
  }

  //
  // Listeners

  onTableOptionsPress = () => {
    this.setState({ isTableOptionsModalOpen: true });
  };

  onTableOptionsModalClose = () => {
    this.setState({ isTableOptionsModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      columns,
      onTableOptionChange,
      allSelected,
      allUnselected,
      onSelectAllChange,
      ...otherProps
    } = this.props;

    return (
      <VirtualTableHeader>
        {
          columns.map((column) => {
            const {
              name,
              label,
              isSortable,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'select') {
              return (
                <VirtualTableSelectAllHeaderCell
                  key={name}
                  allSelected={allSelected}
                  allUnselected={allUnselected}
                  onSelectAllChange={onSelectAllChange}
                />
              );
            }

            if (name === 'actions') {
              return (
                <VirtualTableHeaderCell
                  key={name}
                  className={styles[name]}
                  name={name}
                  isSortable={false}
                  {...otherProps}
                >
                  <IconButton
                    name={icons.ADVANCED_SETTINGS}
                    onPress={this.onTableOptionsPress}
                  />
                </VirtualTableHeaderCell>
              );
            }

            return (
              <VirtualTableHeaderCell
                key={name}
                className={styles[name]}
                name={name}
                isSortable={isSortable}
                {...otherProps}
              >
                {label}
              </VirtualTableHeaderCell>
            );
          })
        }

        <TableOptionsModal
          isOpen={this.state.isTableOptionsModalOpen}
          columns={columns}
          onTableOptionChange={onTableOptionChange}
          onModalClose={this.onTableOptionsModalClose}
        />
      </VirtualTableHeader>
    );
  }
}

SearchIndexHeader.propTypes = {
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  allSelected: PropTypes.bool.isRequired,
  allUnselected: PropTypes.bool.isRequired,
  onSelectAllChange: PropTypes.func.isRequired,
  onTableOptionChange: PropTypes.func.isRequired
};

export default SearchIndexHeader;

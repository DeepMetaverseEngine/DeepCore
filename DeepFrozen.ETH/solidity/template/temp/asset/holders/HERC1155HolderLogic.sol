// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

abstract contract HERC1155HolderLogic is IERC1155Receiver {

    /*
     * bytes4(keccak256('supportsInterface(bytes4)')) == 0x01ffc9a7
     */
    bytes4 private constant _INTERFACE_ID_ERC165 = 0x01ffc9a7;

    function supportsInterface(bytes4 interfaceId) public view virtual override returns (bool) {

        if (interfaceId == this.supportsInterface.selector) {
            return true;
        } else if (interfaceId == 0xffffffff) {
            return false;
        } else if (interfaceId == this.onERC1155Received.selector) {
            return true;
        } else if (interfaceId == this.onERC1155BatchReceived.selector) {
            return true;
        }

        return false;
    }

    function onERC1155Received(address, address, uint256, uint256, bytes memory) public virtual override returns (bytes4) {
        return this.onERC1155Received.selector;
    }

    function onERC1155BatchReceived(address, address, uint256[] memory, uint256[] memory, bytes memory) public virtual override returns (bytes4) {
        return this.onERC1155BatchReceived.selector;
    }
}

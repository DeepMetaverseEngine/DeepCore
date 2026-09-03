// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC165Layout.sol";
import "./HERC165Interface.sol";

contract HERC165Logic is HERC165Layout, HERC165Interface {

    /**
     * @dev See {IERC165-supportsInterface}.
     *
     * Time complexity O(1), guaranteed to always use less than 30 000 gas.
     */
    function supportsInterface(bytes4 interfaceId) external view virtual override returns (bool) {
        return _supportedInterfaces[interfaceId];
    }
}

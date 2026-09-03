// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

interface HERC4610Event {

    /**
     * @dev Emitted when `owner` or `approved account` enables `setDelegator` to manage the `tokenId` token.
     */
    event SetDelegator(address indexed caller, address indexed delegator, uint256 indexed tokenId);

}

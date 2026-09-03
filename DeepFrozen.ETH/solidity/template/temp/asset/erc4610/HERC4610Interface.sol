// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../preclude/Preclude.sol";

import "../erc721/HERC721Interface.sol";
import "./HERC4610Event.sol";

//ERC4610 is an extension of ERC721
interface HERC4610Interface is HERC721Interface, HERC4610Event {

    /**
     * @dev See {IERC4610-setDelegator}.
     */
    function setDelegator(address delegator, uint256 tokenId) external;

    /**
     * @dev See {IERC4610-delegatorOf}.
     */
    function delegatorOf(uint256 tokenId) view external returns (address);

    /**
     * @dev See {IERC4610-safeTransferFrom}.
     */
    function safeTransferFrom(
        address from,
        address to,
        uint256 tokenId,
        bool reserved
    ) external;

}
